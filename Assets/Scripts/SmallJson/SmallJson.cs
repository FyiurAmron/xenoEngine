// Unity SmallJson JSON loader v0.1 by vaxquis
//
// based on
//
// PetaJson v0.5 Copyright (C) 2014 Topten Software (contact@toptensoftware.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this product 
// except in compliance with the License. You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the 
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
// either express or implied. See the License for the specific language governing permissions 
// and limitations under the License.

// Define PETAJSON_NO_DYNAMIC to disable Expando support
//#define PETAJSON_NO_DYNAMIC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections;
using System.Threading;
#if !PETAJSON_NO_DYNAMIC
using System.Dynamic;

#endif

namespace SmallJson {

// Pass to format/write/parse functions to override defaults
[Flags]
public enum JsonOptions {
    None = 0,
    WriteWhitespace = 0x00000001,
    DontWriteWhitespace = 0x00000002,
    StrictParser = 0x00000004,
    NonStrictParser = 0x00000008,
    Flush = 0x00000010,
    AutoSavePreviousVersion = 0x00000020, // Use "SavePreviousVersions" static property
    SavePreviousVersion = 0x00000040, // Always save previous version
}

// API
public static class Json {
    static Json() {
        writeWhitespaceDefault = true;
        strictParserDefault = false;
    }

    // Pretty format default
    public static bool writeWhitespaceDefault { get; set; }

    // Strict parser
    public static bool strictParserDefault { get; set; }

    // Write an object to a text writer
    public static void write( TextWriter w, object o, JsonOptions options = JsonOptions.None ) {
        var writer = new Internal.Writer( w, resolveOptions( options ) );
        writer.writeValue( o );
    }

    static void deleteFile( string filename ) {
        try {
            File.Delete( filename );
        } catch {
            // Don't care
        }
    }

    public static bool savePreviousVersions { get; set; }

    // Write a file atomically by writing to a temp file and then renaming it - prevents corrupted files if crash 
    // in middle of writing file.
    public static void writeFileAtomic( string filename, object o, JsonOptions options = JsonOptions.None,
                                        string backupFilename = null ) {
        var tempName = filename + ".tmp";

        try {
            // Write the temp file
            writeFile( tempName, o, ( options | JsonOptions.Flush ) );

            if ( File.Exists( filename ) ) {
                bool savePreviousVersion = false;

                if ( ( options & JsonOptions.AutoSavePreviousVersion ) != 0 ) {
                    savePreviousVersion = savePreviousVersions;
                } else if ( ( options & JsonOptions.SavePreviousVersion ) != 0 ) {
                    savePreviousVersion = true;
                }

                // Work out backup filename
                if ( savePreviousVersion ) {
                    // Make sure have a backup filename
                    if ( backupFilename == null ) {
                        backupFilename = filename + ".previous";
                    }
                } else {
                    // No backup
                    backupFilename = null;
                }

                // Replace it
                int retry = 0;
                while ( true ) {
                    try {
                        File.Replace( tempName, filename, backupFilename );
                        break;
                    } catch ( IOException x ) {
                        retry++;
                        if ( retry >= 5 ) {
                            throw new IOException(
                                "Failed to replace temp file " + tempName + " with " + filename + " and backup " +
                                backupFilename + ", reason ", x );
                        }

                        Thread.Sleep( 2000 );
                    }
                }
            } else {
                // Rename it
                File.Move( tempName, filename );
            }
        } catch {
            deleteFile( tempName );
            throw;
        }
    }

    // Write an object to a file
    public static void writeFile( string filename, object o, JsonOptions options = JsonOptions.None ) {
        using ( var w = new StreamWriter( filename ) ) {
            write( w, o, options );

            if ( ( options & JsonOptions.Flush ) != 0 ) {
                w.Flush();
                w.BaseStream.Flush();
            }
        }
    }

    // Format an object as a json string
    public static string format( object o, JsonOptions options = JsonOptions.None ) {
        var sw = new StringWriter();
        var writer = new Internal.Writer( sw, resolveOptions( options ) );
        writer.writeValue( o );
        return sw.ToString();
    }

    // Parse an object of specified type from a text reader
    public static object parse( TextReader r, Type type, JsonOptions options = JsonOptions.None ) {
        Internal.Reader reader = null;
        try {
            reader = new Internal.Reader( r, resolveOptions( options ) );
            var retv = reader.parse( type );
            reader.checkEof();
            return retv;
        } catch ( Exception x ) {
            var loc = reader?.currentTokenPosition ?? new JsonLineOffset();
            var ctx = reader?.context;

            throw new JsonParseException( x, ctx, loc );
        }
    }

    // Parse an object of specified type from a text reader
    public static T parse<T>( TextReader r, JsonOptions options = JsonOptions.None ) {
        return (T) parse( r, typeof(T), options );
    }

    // Parse from text reader into an already instantiated object
    public static void parseInto( TextReader r, object into, JsonOptions options = JsonOptions.None ) {
        if ( into == null ) {
            throw new NullReferenceException();
        }

        if ( into.GetType().IsValueType ) {
            throw new InvalidOperationException( "Can't ParseInto a value type" );
        }

        Internal.Reader reader = null;
        try {
            reader = new Internal.Reader( r, resolveOptions( options ) );
            reader.parseInto( into );
            reader.checkEof();
        } catch ( Exception x ) {
            var loc = reader?.currentTokenPosition ?? new JsonLineOffset();
            var ctx = reader?.context;

            throw new JsonParseException( x, ctx, loc );
        }
    }

    // Parse an object of specified type from a file
    public static object parseFile( string filename, Type type, JsonOptions options = JsonOptions.None ) {
        using ( var r = new StreamReader( filename ) ) {
            return parse( r, type, options );
        }
    }

    // Parse an object of specified type from a file
    public static T parseFile<T>( string filename, JsonOptions options = JsonOptions.None ) {
        using ( var r = new StreamReader( filename ) ) {
            return parse<T>( r, options );
        }
    }

    // Parse from file into an already instantied object
    public static void parseFileInto( string filename, object into, JsonOptions options = JsonOptions.None ) {
        using ( var r = new StreamReader( filename ) ) {
            parseInto( r, into, options );
        }
    }

    // Parse an object from a string
    public static object parse( string data, Type type, JsonOptions options = JsonOptions.None ) {
        return parse( new StringReader( data ), type, options );
    }

    // Parse an object from a string
    public static T parse<T>( string data, JsonOptions options = JsonOptions.None ) {
        return parse<T>( new StringReader( data ), options );
    }

    // Parse from string into an already instantiated object
    public static void parseInto( string data, object into, JsonOptions options = JsonOptions.None ) {
        parseInto( new StringReader( data ), into, options );
    }

    // Create a clone of an object
    public static T clone<T>( T source ) {
        return (T) reparse( source.GetType(), source );
    }

    // Create a clone of an object (untyped)
    public static object clone( object source ) {
        return reparse( source.GetType(), source );
    }

    // Clone an object into another instance
    public static void cloneInto( object dest, object source ) {
        reparseInto( dest, source );
    }

    // Reparse an object by writing to a stream and re-reading (possibly
    // as a different type).
    public static object reparse( Type type, object source ) {
        if ( source == null ) {
            return null;
        }

        var ms = new MemoryStream();
        try {
            // Write
            var w = new StreamWriter( ms );
            write( w, source );
            w.Flush();

            // Read
            ms.Seek( 0, SeekOrigin.Begin );
            var r = new StreamReader( ms );
            return parse( r, type );
        } finally {
            ms.Dispose();
        }
    }

    // Typed version of above
    public static T reparse<T>( object source ) {
        return (T) reparse( typeof(T), source );
    }

    // Reparse one object into another object 
    public static void reparseInto( object dest, object source ) {
        var ms = new MemoryStream();
        try {
            // Write
            var w = new StreamWriter( ms );
            write( w, source );
            w.Flush();

            // Read
            ms.Seek( 0, SeekOrigin.Begin );
            var r = new StreamReader( ms );
            parseInto( r, dest );
        } finally {
            ms.Dispose();
        }
    }

    // Register a callback that can format a value of a particular type into json
    public static void registerFormatter( Type type, Action<IJsonWriter, object> formatter ) {
        Internal.Writer.FORMATTERS[type] = formatter;
    }

    // Typed version of above
    public static void registerFormatter<T>( Action<IJsonWriter, T> formatter ) {
        registerFormatter( typeof(T), ( w, o ) => formatter( w, (T) o ) );
    }

    // Register a parser for a specified type
    public static void registerParser( Type type, Func<IJsonReader, Type, object> parser ) {
        Internal.Reader.PARSERS.set( type, parser );
    }

    // Register a typed parser
    public static void registerParser<T>( Func<IJsonReader, Type, T> parser ) {
        registerParser( typeof(T), ( r, t ) => parser( r, t ) );
    }

    // Simpler version for simple types
    public static void registerParser( Type type, Func<object, object> parser ) {
        registerParser( type, ( r, t ) => r.readLiteral( parser ) );
    }

    // Simpler and typesafe parser for simple types
    public static void registerParser<T>( Func<object, T> parser ) {
        registerParser( typeof(T), literal => parser( literal ) );
    }

    // Register an into parser
    public static void registerIntoParser( Type type, Action<IJsonReader, object> parser ) {
        Internal.Reader.INTO_PARSERS.set( type, parser );
    }

    // Register an into parser
    public static void registerIntoParser<T>( Action<IJsonReader, object> parser ) {
        registerIntoParser( typeof(T), parser );
    }

    // Register a factory for instantiating objects (typically abstract classes)
    // Callback will be invoked for each key in the dictionary until it returns an object
    // instance and which point it will switch to serialization using reflection
    public static void registerTypeFactory( Type type, Func<IJsonReader, string, object> factory ) {
        Internal.Reader.TYPE_FACTORIES.set( type, factory );
    }

    // Register a callback to provide a formatter for a newly encountered type
    public static void setFormatterResolver( Func<Type, Action<IJsonWriter, object>> resolver ) {
        Internal.Writer.formatterResolver = resolver;
    }

    // Register a callback to provide a parser for a newly encountered value type
    public static void setParserResolver( Func<Type, Func<IJsonReader, Type, object>> resolver ) {
        Internal.Reader.parserResolver = resolver;
    }

    // Register a callback to provide a parser for a newly encountered reference type
    public static void setIntoParserResolver( Func<Type, Action<IJsonReader, object>> resolver ) {
        Internal.Reader.intoParserResolver = resolver;
    }

    public static bool walkPath( this IDictionary<string, object> This, string path, bool create,
                                 Func<IDictionary<string, object>, string, bool> leafCallback ) {
        // Walk the path
        var parts = path.Split( '.' );
        for ( int i = 0; i < parts.Length - 1; i++ ) {
            if ( !This.TryGetValue( parts[i], out object val ) ) {
                if ( !create ) {
                    return false;
                }

                val = new Dictionary<string, object>();
                This[parts[i]] = val;
            }

            This = (IDictionary<string, object>) val;
        }

        // Process the leaf
        return leafCallback( This, parts[parts.Length - 1] );
    }

    public static bool pathExists( this IDictionary<string, object> This, string path ) {
        return This.walkPath( path, false, ( dict, key ) => dict.ContainsKey( key ) );
    }

    public static object getPath( this IDictionary<string, object> This, Type type, string path, object def ) {
        This.walkPath( path, false, ( dict, key ) => {
            if ( !dict.TryGetValue( key, out object val ) ) {
                return true;
            }

            if ( val == null || type.IsInstanceOfType( val ) ) {
                def = val;
            } else {
                def = reparse( type, val );
            }

            return true;
        } );

        return def;
    }

    // Ensure there's an object of type T at specified path
    public static T getObjectAtPath<T>( this IDictionary<string, object> This, string path ) where T : class, new() {
        T retVal = null;
        This.walkPath( path, true, ( dict, key ) => {
            object val;
            dict.TryGetValue( key, out val );
            retVal = val as T;
            if ( retVal == null ) {
                retVal = val == null ? new T() : reparse<T>( val );
                dict[key] = retVal;
            }

            return true;
        } );

        return retVal;
    }

    public static T getPath<T>( this IDictionary<string, object> This, string path, T def = default(T) ) {
        return (T) This.getPath( typeof(T), path, def );
    }

    public static void setPath( this IDictionary<string, object> This, string path, object value ) {
        This.walkPath( path, true, ( dict, key ) => {
            dict[key] = value;
            return true;
        } );
    }

    // Resolve passed options        
    static JsonOptions resolveOptions( JsonOptions options ) {
        JsonOptions resolved = JsonOptions.None;

        if ( ( options & ( JsonOptions.WriteWhitespace | JsonOptions.DontWriteWhitespace ) ) != 0 ) {
            resolved |= options & ( JsonOptions.WriteWhitespace | JsonOptions.DontWriteWhitespace );
        } else {
            resolved |= writeWhitespaceDefault ? JsonOptions.WriteWhitespace : JsonOptions.DontWriteWhitespace;
        }

        if ( ( options & ( JsonOptions.StrictParser | JsonOptions.NonStrictParser ) ) != 0 ) {
            resolved |= options & ( JsonOptions.StrictParser | JsonOptions.NonStrictParser );
        } else {
            resolved |= strictParserDefault ? JsonOptions.StrictParser : JsonOptions.NonStrictParser;
        }

        return resolved;
    }
}

// Called before loading via reflection
[Obfuscation( Exclude = true, ApplyToMembers = true )]
public interface IJsonLoading {
    void onJsonLoading( IJsonReader r );
}

// Called after loading via reflection
[Obfuscation( Exclude = true, ApplyToMembers = true )]
public interface IJsonLoaded {
    void onJsonLoaded( IJsonReader r );
}

// Called for each field while loading from reflection
// Return true if handled
[Obfuscation( Exclude = true, ApplyToMembers = true )]
public interface IJsonLoadField {
    bool onJsonField( IJsonReader r, string key );
}

// Called when about to write using reflection
[Obfuscation( Exclude = true, ApplyToMembers = true )]
public interface IJsonWriting {
    void onJsonWriting( IJsonWriter w );
}

// Called after written using reflection
[Obfuscation( Exclude = true, ApplyToMembers = true )]
public interface IJsonWritten {
    void onJsonWritten( IJsonWriter w );
}

// Describes the current literal in the json stream
public enum LiteralKind {
    None,
    String,
    Null,
    True,
    False,
    SignedInteger,
    UnsignedInteger,
    FloatingPoint,
}

[Obfuscation( Exclude = true, ApplyToMembers = true )]
public enum Token {
    Eof,
    Identifier,
    Literal,
    OpenBrace,
    CloseBrace,
    OpenSquare,
    CloseSquare,
    Equal,
    Colon,
    SemiColon,
    Comma,
}

// Passed to registered parsers
[Obfuscation( Exclude = true, ApplyToMembers = true )]
public interface IJsonReader {
    object parse( Type type );
    T parse<T>();
    void parseInto( object into );

    Token currentToken { get; }
    object readLiteral( Func<object, object> converter );
    void parseDictionary( Action<string> callback );
    void parseArray( Action callback );

    LiteralKind getLiteralKind();
    string getLiteralString();
    void nextToken();
}

// Passed to registered formatters
[Obfuscation( Exclude = true, ApplyToMembers = true )]
public interface IJsonWriter {
    void writeStringLiteral( string str );
    void writeRaw( string str );
    void writeArray( Action callback );
    void writeDictionary( Action callback );
    void writeValue( object value );
    void writeElement();
    void writeKey( string key );
    void writeKeyNoEscaping( string key );
}

// Exception thrown for any parse error
public class JsonParseException : Exception {
    public JsonParseException( Exception inner, string context, JsonLineOffset position ) :
        base(
            "JSON parse error at " + position +
            ( string.IsNullOrEmpty( context ) ? "" : ", context " + context )
            + " - " + inner.Message,
            inner ) {
        this.position = position;
        this.context = context;
    }

    public JsonLineOffset position;
    public string context;
}

// Represents a line and character offset position in the source Json
public struct JsonLineOffset {
    public int line;
    public int offset;

    public override string ToString() {
        return "line " + ( line + 1 ) + ", character " + ( offset + 1 );
    }
}

// Used to decorate fields and properties that should be serialized
//
// - [Json] on class or struct causes all public fields and properties to be serialized
// - [Json] on a public or non-public field or property causes that member to be serialized
// - [JsonExclude] on a field or property causes that field to be not serialized
// - A class or struct with no [Json] attribute has all public fields/properties serialized
// - A class or struct with no [Json] attribute but a [Json] attribute on one or more members only serializes those members
//
// Use [Json("keyname")] to explicitly specify the key to be used 
// [Json] without the keyname will be serialized using the name of the member with the first letter lowercased.
//
// [Json(KeepInstance=true)] causes container/subobject types to be serialized into the existing member instance (if not null)
//
[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property |
                 AttributeTargets.Field )]
public class JsonAttribute : Attribute {
    public JsonAttribute() {
        key = null;
    }

    public JsonAttribute( string key ) {
        this.key = key;
    }

    // Key used to save this field/property
    public string key { get; }

    // If true uses ParseInto to parse into the existing object instance
    // If false, creates a new instance as assigns it to the property
    public bool keepInstance { get; set; }

    // If true, the property will be loaded, but not saved
    // Use to upgrade deprecated persisted settings, but not
    // write them back out again
    public bool deprecated { get; set; }
}

// See comments for JsonAttribute above
[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field )]
public class JsonExcludeAttribute : Attribute {
    public JsonExcludeAttribute() {
    }
}

// Apply to enum values to specify which enum value to select
// if the supplied json value doesn't match any.
// If not found throws an exception
// eg, any unknown values in the json will be mapped to Fruit.unknown
//
//	 [JsonUnknown(Fruit.unknown)]
//   enum Fruit
//   {
// 		unknown,
//      Apple,
//      Pear,
//	 }
[AttributeUsage( AttributeTargets.Enum )]
public class JsonUnknownAttribute : Attribute {
    public JsonUnknownAttribute( object unknownValue ) {
        this.unknownValue = unknownValue;
    }

    public object unknownValue { get; private set; }
}

namespace Internal {

class CancelReaderException : Exception {
}

// Helper to create instances but include the type name in the thrown exception
public static class DecoratingActivator {
    public static object createInstance( Type t ) {
        try {
            return Activator.CreateInstance( t );
        } catch ( Exception x ) {
            throw new InvalidOperationException( $"Failed to create instance of type '" + t.FullName + "'", x );
        }
    }
}

public class Reader : IJsonReader {
    static Reader() {
        // Setup default resolvers
        parserResolver = resolveParser;
        intoParserResolver = resolveIntoParser;

        object simpleConverter( IJsonReader reader, Type type ) {
            return reader.readLiteral( literal => Convert.ChangeType( literal, type, CultureInfo.InvariantCulture ) );
        }

        object numberConverter( IJsonReader reader, Type type ) {
            switch ( reader.getLiteralKind() ) {
                case LiteralKind.SignedInteger:
                case LiteralKind.UnsignedInteger: {
                    var str = reader.getLiteralString();
                    if ( str.StartsWith( "0x", StringComparison.InvariantCultureIgnoreCase ) ) {
                        var tempValue = Convert.ToUInt64( str.Substring( 2 ), 16 );
                        object val = Convert.ChangeType( tempValue, type, CultureInfo.InvariantCulture );
                        reader.nextToken();
                        return val;
                    } else {
                        object val = Convert.ChangeType( str, type, CultureInfo.InvariantCulture );
                        reader.nextToken();
                        return val;
                    }
                }

                case LiteralKind.FloatingPoint: {
                    object val = Convert.ChangeType( reader.getLiteralString(), type, CultureInfo.InvariantCulture );
                    reader.nextToken();
                    return val;
                }
            }

            throw new InvalidDataException( "expected a numeric literal" );
        }

        // Default type handlers
        PARSERS.set( typeof(string), simpleConverter );
        PARSERS.set( typeof(char), simpleConverter );
        PARSERS.set( typeof(bool), simpleConverter );
        PARSERS.set( typeof(byte), numberConverter );
        PARSERS.set( typeof(sbyte), numberConverter );
        PARSERS.set( typeof(short), numberConverter );
        PARSERS.set( typeof(ushort), numberConverter );
        PARSERS.set( typeof(int), numberConverter );
        PARSERS.set( typeof(uint), numberConverter );
        PARSERS.set( typeof(long), numberConverter );
        PARSERS.set( typeof(ulong), numberConverter );
        PARSERS.set( typeof(decimal), numberConverter );
        PARSERS.set( typeof(float), numberConverter );
        PARSERS.set( typeof(double), numberConverter );
        PARSERS.set( typeof(DateTime),
                     ( reader, type ) => {
                         return reader.readLiteral( literal => Utils.fromUnixMilliseconds(
                                                        (long) Convert.ChangeType(
                                                            literal, typeof(long), CultureInfo.InvariantCulture ) ) );
                     } );
        PARSERS.set( typeof(byte[]), ( reader, type ) => {
            if ( reader.currentToken == Token.OpenSquare ) {
                throw new CancelReaderException();
            }

            return reader.readLiteral( literal => Convert.FromBase64String(
                                           (string) Convert.ChangeType( literal, typeof(string),
                                                                        CultureInfo.InvariantCulture ) ) );
        } );
    }

    public Reader( TextReader r, JsonOptions options ) {
        tokenizer = new Tokenizer( r, options );
        this.options = options;
    }

    private readonly Tokenizer tokenizer;
    public readonly JsonOptions options;
    private readonly List<string> contextStack = new List<string>();

    public string context => string.Join( ".", contextStack );

    private static Action<IJsonReader, object> resolveIntoParser( Type type ) {
        var ri = ReflectionInfo.getReflectionInfo( type );
        if ( ri != null ) {
            return ri.parseInto;
        } else {
            return null;
        }
    }

    private static Func<IJsonReader, Type, object> resolveParser( Type type ) {
        // See if the Type has a static parser method - T ParseJson(IJsonReader)
        var parseJson = ReflectionInfo.findParseJson( type );
        if ( parseJson == null ) {
            return ( r, t ) => {
                var into = DecoratingActivator.createInstance( type );
                r.parseInto( @into );
                return @into;
            };
        }

        if ( parseJson.GetParameters()[0].ParameterType == typeof(IJsonReader) ) {
            return ( r, t ) => parseJson.Invoke( null, new object[] {r} );
        }

        return ( r, t ) => {
            if ( r.getLiteralKind() != LiteralKind.String ) {
                throw new InvalidDataException( "Expected string literal for type " + type.FullName );
            }

            var o = parseJson.Invoke( null, new object[] {r.getLiteralString()} );
            r.nextToken();
            return o;
        };
    }

    public JsonLineOffset currentTokenPosition {
        get { return tokenizer.currentTokenPosition; }
    }

    public Token currentToken {
        get { return tokenizer.currentToken; }
    }

    // ReadLiteral is implemented with a converter callback so that any
    // errors on converting to the target type are thrown before the tokenizer
    // is advanced to the next token.  This ensures error location is reported 
    // at the start of the literal, not the following token.
    public object readLiteral( Func<object, object> converter ) {
        tokenizer.check( Token.Literal );
        var retv = converter( tokenizer.literalValue );
        tokenizer.nextToken();
        return retv;
    }

    public void checkEof() {
        tokenizer.check( Token.Eof );
    }

    public object parse( Type type ) {
        // Null?
        if ( tokenizer.currentToken == Token.Literal && tokenizer.literalKind == LiteralKind.Null ) {
            tokenizer.nextToken();
            return null;
        }

        // Handle nullable types
        var typeUnderlying = Nullable.GetUnderlyingType( type );
        if ( typeUnderlying != null ) {
            type = typeUnderlying;
        }

        // See if we have a reader
        if ( PARSERS.tryGetValue( type, out var parser ) ) {
            try {
                return parser( this, type );
            } catch ( CancelReaderException ) {
                // Reader aborted trying to read this format
            }
        }

        // See if we have factory
        if ( TYPE_FACTORIES.tryGetValue( type, out var factory ) ) {
            // Try first without passing dictionary keys
            object into = factory( this, null );
            if ( into == null ) {
                // This is a awkward situation.  The factory requires a value from the dictionary
                // in order to create the target object (typically an abstract class with the class
                // kind recorded in the Json).  Since there's no guarantee of order in a json dictionary
                // we can't assume the required key is first.
                // So, create a bookmark on the tokenizer, read keys until the factory returns an
                // object instance and then rewind the tokenizer and continue

                // Create a bookmark so we can rewind
                tokenizer.createBookmark();

                // Skip the opening brace
                tokenizer.skip( Token.OpenBrace );

                // First pass to work out type
                parseDictionaryKeys( key => {
                    // Try to instantiate the object
                    into = factory( this, key );
                    return into == null;
                } );

                // Move back to start of the dictionary
                tokenizer.rewindToBookmark();

                // Quit if still didn't get an object from the factory
                if ( into == null ) {
                    throw new InvalidOperationException(
                        "Factory didn't create object instance (probably due to a missing key in the Json)" );
                }
            }

            // Second pass
            parseInto( into );

            // Done
            return into;
        }

        // Do we already have an into parser?
        if ( INTO_PARSERS.tryGetValue( type, out var intoParser ) ) {
            var into = DecoratingActivator.createInstance( type );
            parseInto( into );
            return into;
        }

        // Enumerated type?
        if ( type.IsEnum ) {
            if ( type.GetCustomAttributes( typeof(FlagsAttribute), false ).Any() ) {
                return readLiteral( literal => {
                    try {
                        return Enum.Parse( type, (string) literal );
                    } catch {
                        return Enum.ToObject( type, literal );
                    }
                } );
            } else {
                return readLiteral( literal => {
                    try {
                        return Enum.Parse( type, (string) literal );
                    } catch ( Exception ) {
                        var attr = type.GetCustomAttributes( typeof(JsonUnknownAttribute), false ).FirstOrDefault();
                        if ( attr == null ) {
                            throw;
                        }

                        return ( (JsonUnknownAttribute) attr ).unknownValue;
                    }
                } );
            }
        }

        // Array?
        if ( type.IsArray && type.GetArrayRank() == 1 ) {
            // First parse as a List<>
            var listType = typeof(List<>).MakeGenericType( type.GetElementType() );
            var list = DecoratingActivator.createInstance( listType );
            parseInto( list );

            return listType.GetMethod( "ToArray" )?.Invoke( list, null );
        }

        // IEnumerable
        if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ) {
            // First parse as a List<>
            var declType = type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType( declType );
            var list = DecoratingActivator.createInstance( listType );
            parseInto( list );

            return list;
        }

        // Convert interfaces to concrete types
        if ( type.IsInterface ) {
            type = Utils.resolveInterfaceToClass( type );
        }

        // Untyped dictionary?
        if ( tokenizer.currentToken == Token.OpenBrace &&
             ( type.IsAssignableFrom( typeof(IDictionary<string, object>) ) ) ) {
#if !PETAJSON_NO_DYNAMIC
            var container = ( new ExpandoObject() ) as IDictionary<string, object>;
#else
            var container = new Dictionary<string, object>();
#endif
            parseDictionary( key => { container[key] = parse( typeof(object) ); } );

            return container;
        }

        // Untyped list?
        if ( tokenizer.currentToken == Token.OpenSquare && ( type.IsAssignableFrom( typeof(List<object>) ) ) ) {
            var container = new List<object>();
            parseArray( () => { container.Add( parse( typeof(object) ) ); } );
            return container;
        }

        // Untyped literal?
        if ( tokenizer.currentToken == Token.Literal && type.IsAssignableFrom( tokenizer.literalType ) ) {
            var lit = tokenizer.literalValue;
            tokenizer.nextToken();
            return lit;
        }

        // Call value type resolver
        if ( type.IsValueType ) {
            var tp = PARSERS.get( type, () => parserResolver( type ) );
            if ( tp != null ) {
                return tp( this, type );
            }
        }

        // Call reference type resolver
        if ( type.IsClass && type != typeof(object) ) {
            var into = DecoratingActivator.createInstance( type );
            parseInto( into );
            return into;
        }

        // Give up
        throw new InvalidDataException( "syntax error, unexpected token " + tokenizer.currentToken );
    }

    // Parse into an existing object instance
    public void parseInto( object into ) {
        if ( into == null ) {
            return;
        }

        if ( tokenizer.currentToken == Token.Literal && tokenizer.literalKind == LiteralKind.Null ) {
            throw new InvalidOperationException( "can't parse null into existing instance" );
            //return;
        }

        var type = into.GetType();

        // Existing parse into handler?
        if ( INTO_PARSERS.tryGetValue( type, out var parseInto ) ) {
            parseInto( this, into );
            return;
        }

        // Generic dictionary?
        var dictType = Utils.findGenericInterface( type, typeof(IDictionary<,>) );
        if ( dictType != null ) {
            // Get the key and value types
            var typeKey = dictType.GetGenericArguments()[0];
            var typeValue = dictType.GetGenericArguments()[1];

            // Parse it
            IDictionary dict = (IDictionary) into;
            dict.Clear();
            parseDictionary( key => { dict.Add( Convert.ChangeType( key, typeKey ), parse( typeValue ) ); } );

            return;
        }

        // Generic list
        var listType = Utils.findGenericInterface( type, typeof(IList<>) );
        if ( listType != null ) {
            // Get element type
            var typeElement = listType.GetGenericArguments()[0];

            // Parse it
            IList list = (IList) into;
            list.Clear();
            parseArray( () => { list.Add( parse( typeElement ) ); } );

            return;
        }

        // Untyped dictionary
        if ( into is IDictionary objDict ) {
            objDict.Clear();
            parseDictionary( key => { objDict[key] = parse( typeof(object) ); } );
            return;
        }

        // Untyped list
        if ( into is IList objList ) {
            objList.Clear();
            parseArray( () => { objList.Add( parse( typeof(object) ) ); } );
            return;
        }

        // Try to resolve a parser
        var intoParser = INTO_PARSERS.get( type, () => intoParserResolver( type ) );
        if ( intoParser == null ) {
            throw new InvalidOperationException(
                "Don't know how to parse into type '" + type.FullName + "'" );
        }

        intoParser( this, into );
    }

    public T parse<T>() {
        return (T) parse( typeof(T) );
    }

    public LiteralKind getLiteralKind() {
        return tokenizer.literalKind;
    }

    public string getLiteralString() {
        return tokenizer.String;
    }

    public void nextToken() {
        tokenizer.nextToken();
    }

    // Parse a dictionary
    public void parseDictionary( Action<string> callback ) {
        tokenizer.skip( Token.OpenBrace );
        parseDictionaryKeys( key => {
            callback( key );
            return true;
        } );
        tokenizer.skip( Token.CloseBrace );
    }

    // Parse dictionary keys, calling callback for each one.  Continues until end of input
    // or when callback returns false
    private void parseDictionaryKeys( Func<string, bool> callback ) {
        // End?
        while ( tokenizer.currentToken != Token.CloseBrace ) {
            // Parse the key
            string key;
            if ( tokenizer.currentToken == Token.Identifier && ( options & JsonOptions.StrictParser ) == 0 ) {
                key = tokenizer.String;
            } else if ( tokenizer.currentToken == Token.Literal && tokenizer.literalKind == LiteralKind.String ) {
                key = (string) tokenizer.literalValue;
            } else {
                throw new InvalidDataException( "syntax error, expected string literal or identifier" );
            }

            tokenizer.nextToken();
            tokenizer.skip( Token.Colon );

            // Remember current position
            var pos = tokenizer.currentTokenPosition;

            // Call the callback, quit if cancelled
            contextStack.Add( key );
            bool doDefaultProcessing = callback( key );
            contextStack.RemoveAt( contextStack.Count - 1 );
            if ( !doDefaultProcessing ) {
                return;
            }

            // If the callback didn't read anything from the tokenizer, then skip it by ourselves
            if ( pos.line == tokenizer.currentTokenPosition.line &&
                 pos.offset == tokenizer.currentTokenPosition.offset ) {
                parse( typeof(object) );
            }

            // Separating/trailing comma
            if ( tokenizer.skipIf( Token.Comma ) ) {
                if ( ( options & JsonOptions.StrictParser ) != 0 && tokenizer.currentToken == Token.CloseBrace ) {
                    throw new InvalidDataException( "Trailing commas not allowed in strict mode" );
                }

                continue;
            }

            // End
            break;
        }
    }

    // Parse an array
    public void parseArray( Action callback ) {
        tokenizer.skip( Token.OpenSquare );

        int index = 0; // WTF??
        while ( tokenizer.currentToken != Token.CloseSquare ) {
            contextStack.Add( "[" + index + "]" );
            callback();
            contextStack.RemoveAt( contextStack.Count - 1 );

            if ( tokenizer.skipIf( Token.Comma ) ) {
                if ( ( options & JsonOptions.StrictParser ) != 0 && tokenizer.currentToken == Token.CloseSquare ) {
                    throw new InvalidDataException( "Trailing commas not allowed in strict mode" );
                }

                continue;
            }

            break;
        }

        tokenizer.skip( Token.CloseSquare );
    }

    // Yikes!
    public static Func<Type, Action<IJsonReader, object>> intoParserResolver;
    public static Func<Type, Func<IJsonReader, Type, object>> parserResolver;

    public static readonly ThreadSafeCache<Type, Func<IJsonReader, Type, object>> PARSERS =
        new ThreadSafeCache<Type, Func<IJsonReader, Type, object>>();

    public static readonly ThreadSafeCache<Type, Action<IJsonReader, object>> INTO_PARSERS =
        new ThreadSafeCache<Type, Action<IJsonReader, object>>();

    public static readonly ThreadSafeCache<Type, Func<IJsonReader, string, object>> TYPE_FACTORIES =
        new ThreadSafeCache<Type, Func<IJsonReader, string, object>>();
}

public class Writer : IJsonWriter {
    static Writer() {
        formatterResolver = resolveFormatter;

        // Register standard formatters
        FORMATTERS.Add( typeof(string), ( w, o ) => w.writeStringLiteral( (string) o ) );
        FORMATTERS.Add( typeof(char), ( w, o ) => w.writeStringLiteral( ( (char) o ).ToString() ) );
        FORMATTERS.Add( typeof(bool), ( w, o ) => w.writeRaw( ( (bool) o ) ? "true" : "false" ) );

        void convertWriter( IJsonWriter w, object o )
            => w.writeRaw( (string) Convert.ChangeType( o, typeof(string), CultureInfo.InvariantCulture ) );

        FORMATTERS.Add( typeof(int), convertWriter );
        FORMATTERS.Add( typeof(uint), convertWriter );
        FORMATTERS.Add( typeof(long), convertWriter );
        FORMATTERS.Add( typeof(ulong), convertWriter );
        FORMATTERS.Add( typeof(short), convertWriter );
        FORMATTERS.Add( typeof(ushort), convertWriter );
        FORMATTERS.Add( typeof(decimal), convertWriter );
        FORMATTERS.Add( typeof(byte), convertWriter );
        FORMATTERS.Add( typeof(sbyte), convertWriter );
        FORMATTERS.Add( typeof(DateTime), ( w, o ) => convertWriter( w, Utils.toUnixMilliseconds( (DateTime) o ) ) );
        FORMATTERS.Add( typeof(float),
                        ( w, o ) => w.writeRaw(
                            ( (float) o ).ToString( "R", CultureInfo.InvariantCulture ) ) );
        FORMATTERS.Add( typeof(double),
                        ( w, o ) => w.writeRaw(
                            ( (double) o ).ToString( "R", CultureInfo.InvariantCulture ) ) );
        FORMATTERS.Add( typeof(byte[]), ( w, o ) => {
            w.writeRaw( "\"" );
            w.writeRaw( Convert.ToBase64String( (byte[]) o ) );
            w.writeRaw( "\"" );
        } );
    }

    public static Func<Type, Action<IJsonWriter, object>> formatterResolver;

    public static readonly Dictionary<Type, Action<IJsonWriter, object>> FORMATTERS =
        new Dictionary<Type, Action<IJsonWriter, object>>();

    static Action<IJsonWriter, object> resolveFormatter( Type type ) {
        // Try `void FormatJson(IJsonWriter)`
        var formatJson = ReflectionInfo.findFormatJson( type );
        if ( formatJson != null ) {
            if ( formatJson.ReturnType == typeof(void) ) {
                return ( w, obj ) => formatJson.Invoke( obj, new object[] {w} );
            }

            if ( formatJson.ReturnType == typeof(string) ) {
                return ( w, obj ) => w.writeStringLiteral( (string) formatJson.Invoke( obj, new object[] { } ) );
            }
        }

        var ri = ReflectionInfo.getReflectionInfo( type );
        return ( ri == null )
            ? null
            : (Action<IJsonWriter, object>) ri.write;
    }

    public Writer( TextWriter w, JsonOptions options ) {
        writer = w;
        atStartOfLine = true;
        needElementSeparator = false;
        this.options = options;
    }

    private readonly TextWriter writer;
    private int indentLevel;
    private bool atStartOfLine;
    private bool needElementSeparator = false;
    private readonly JsonOptions options;
    private char currentBlockKind = '\0';

    // Move to the next line
    public void nextLine() {
        if ( atStartOfLine ) {
            return;
        }

        if ( ( options & JsonOptions.WriteWhitespace ) != 0 ) {
            writeRaw( "\n" );
            writeRaw( new string( '\t', indentLevel ) );
        }

        atStartOfLine = true;
    }

    // Start the next element, writing separators and white space
    void nextElement() {
        if ( needElementSeparator ) {
            writeRaw( "," );
            nextLine();
        } else {
            nextLine();
            indentLevel++;
            writeRaw( currentBlockKind.ToString() );
            nextLine();
        }

        needElementSeparator = true;
    }

    // Write next array element
    public void writeElement() {
        if ( currentBlockKind != '[' ) {
            throw new InvalidOperationException( "Attempt to write array element when not in array block" );
        }

        nextElement();
    }

    // Write next dictionary key
    public void writeKey( string key ) {
        if ( currentBlockKind != '{' ) {
            throw new InvalidOperationException( "Attempt to write dictionary element when not in dictionary block" );
        }

        nextElement();
        writeStringLiteral( key );
        writeRaw( ( ( options & JsonOptions.WriteWhitespace ) != 0 ) ? ": " : ":" );
    }

    // Write an already escaped dictionary key
    public void writeKeyNoEscaping( string key ) {
        if ( currentBlockKind != '{' ) {
            throw new InvalidOperationException( "Attempt to write dictionary element when not in dictionary block" );
        }

        nextElement();
        writeRaw( "\"" );
        writeRaw( key );
        writeRaw( "\"" );
        writeRaw( ( ( options & JsonOptions.WriteWhitespace ) != 0 ) ? ": " : ":" );
    }

    // Write anything
    public void writeRaw( string str ) {
        atStartOfLine = false;
        writer.Write( str );
    }

    static int indexOfEscapeableChar( string str, int pos ) {
        int length = str.Length;
        while ( pos < length ) {
            var ch = str[pos];
            if ( ch == '\\' || ch == '/' || ch == '\"' || ( ch >= 0 && ch <= 0x1f ) || ( ch >= 0x7f && ch <= 0x9f ) ||
                 ch == 0x2028 || ch == 0x2029 ) {
                return pos;
            }

            pos++;
        }

        return -1;
    }

    public void writeStringLiteral( string str ) {
        atStartOfLine = false;
        if ( str == null ) {
            writer.Write( "null" );
            return;
        }

        writer.Write( "\"" );

        int pos = 0;
        int escapePos;
        while ( ( escapePos = indexOfEscapeableChar( str, pos ) ) >= 0 ) {
            if ( escapePos > pos ) {
                writer.Write( str.Substring( pos, escapePos - pos ) );
            }

            switch ( str[escapePos] ) {
                case '\"':
                    writer.Write( "\\\"" );
                    break;
                case '\\':
                    writer.Write( "\\\\" );
                    break;
                case '/':
                    writer.Write( "\\/" );
                    break;
                case '\b':
                    writer.Write( "\\b" );
                    break;
                case '\f':
                    writer.Write( "\\f" );
                    break;
                case '\n':
                    writer.Write( "\\n" );
                    break;
                case '\r':
                    writer.Write( "\\r" );
                    break;
                case '\t':
                    writer.Write( "\\t" );
                    break;
                default:
                    writer.Write( "\\u{0:x4}", (int) str[escapePos] );
                    break;
            }

            pos = escapePos + 1;
        }

        if ( str.Length > pos ) {
            writer.Write( str.Substring( pos ) );
        }

        writer.Write( "\"" );
    }

    // Write an array or dictionary block
    private void writeBlock( string open, string close, Action callback ) {
        var prevBlockKind = currentBlockKind;
        currentBlockKind = open[0];

        var didNeedElementSeparator = needElementSeparator;
        needElementSeparator = false;

        callback();

        if ( needElementSeparator ) {
            indentLevel--;
            nextLine();
        } else {
            writeRaw( open );
        }

        writeRaw( close );

        needElementSeparator = didNeedElementSeparator;
        currentBlockKind = prevBlockKind;
    }

    // Write an array
    public void writeArray( Action callback ) {
        writeBlock( "[", "]", callback );
    }

    // Write a dictionary
    public void writeDictionary( Action callback ) {
        writeBlock( "{", "}", callback );
    }

    // Write any value
    public void writeValue( object value ) {
        atStartOfLine = false;

        // Special handling for null
        if ( value == null ) {
            writer.Write( "null" );
            return;
        }

        var type = value.GetType();

        // Handle nullable types
        var typeUnderlying = Nullable.GetUnderlyingType( type );
        if ( typeUnderlying != null ) {
            type = typeUnderlying;
        }

        // Look up type writer
        if ( FORMATTERS.TryGetValue( type, out var typeWriter ) ) {
            // Write it
            typeWriter( this, value );
            return;
        }

        // Enumerated type?
        if ( type.IsEnum ) {
            if ( type.GetCustomAttributes( typeof(FlagsAttribute), false ).Any() ) {
                writeRaw( Convert.ToUInt32( value ).ToString( CultureInfo.InvariantCulture ) );
            } else {
                writeStringLiteral( value.ToString() );
            }

            return;
        }

        // Dictionary?
        if ( value is IDictionary d ) {
            writeDictionary( () => {
                foreach ( var key in d.Keys ) {
                    writeKey( key.ToString() );
                    writeValue( d[key] );
                }
            } );
            return;
        }

        // Dictionary?
        if ( value is IDictionary<string, object> dso ) {
            writeDictionary( () => {
                foreach ( var key in dso.Keys ) {
                    writeKey( key );
                    writeValue( dso[key] );
                }
            } );
            return;
        }

        if ( value is IEnumerable e ) {
            writeArray( () => {
                foreach ( var i in e ) {
                    writeElement();
                    writeValue( i );
                }
            } );
            return;
        }

        // Resolve a formatter
        var formatter = formatterResolver( type );
        if ( formatter != null ) {
            FORMATTERS[type] = formatter;
            formatter( this, value );
            return;
        }

        // Give up
        throw new InvalidDataException( "Don't know how to write '" + value.GetType() + "' to json" );
    }
}

// Information about a field or property found through reflection
public class JsonMemberInfo {
    // The Json key for this member
    public string jsonKey;

    // True if should keep existing instance (reference types only)
    public bool keepInstance;

    // True if deprecated
    public bool deprecated;

    // Reflected member info
    private MemberInfo mi;

    public MemberInfo member {
        get => mi;
        set {
            // Store it
            mi = value;

            // Also create getters and setters
            if ( mi is PropertyInfo ) {
                getValue = ( obj ) => ( (PropertyInfo) mi ).GetValue( obj, null );
                setValue = ( obj, val ) => ( (PropertyInfo) mi ).SetValue( obj, val, null );
            } else {
                getValue = ( (FieldInfo) mi ).GetValue;
                setValue = ( (FieldInfo) mi ).SetValue;
            }
        }
    }

    // Member type
    public Type memberType =>
        ( member is PropertyInfo )
            ? ( (PropertyInfo) member ).PropertyType
            : ( (FieldInfo) member ).FieldType;

    // Get/set helpers
    public Action<object, object> setValue;
    public Func<object, object> getValue;
}

// Stores reflection info about a type
public class ReflectionInfo {
    // List of members to be serialized
    public List<JsonMemberInfo> members;

    // Cache of these ReflectionInfos's
    private static readonly ThreadSafeCache<Type, ReflectionInfo> CACHE = new ThreadSafeCache<Type, ReflectionInfo>();

    public static MethodInfo findFormatJson( Type type ) {
        if ( type.IsValueType ) {
            // Try `void FormatJson(IJsonWriter)`
            var formatJson = type.GetMethod( "FormatJson",
                                             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                                             new Type[] {typeof(IJsonWriter)}, null );
            if ( formatJson != null && formatJson.ReturnType == typeof(void) ) {
                return formatJson;
            }

            // Try `string FormatJson()`
            formatJson = type.GetMethod( "FormatJson",
                                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                                         new Type[] { }, null );
            if ( formatJson != null && formatJson.ReturnType == typeof(string) ) {
                return formatJson;
            }
        }

        return null;
    }

    public static MethodInfo findParseJson( Type type ) {
        // Try `T ParseJson(IJsonReader)`
        var parseJson = type.GetMethod( "ParseJson", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                                        null, new[] {typeof(IJsonReader)}, null );
        if ( parseJson != null && parseJson.ReturnType == type ) {
            return parseJson;
        }

        // Try `T ParseJson(string)`
        parseJson = type.GetMethod( "ParseJson", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                                    null, new[] {typeof(string)}, null );
        if ( parseJson != null && parseJson.ReturnType == type ) {
            return parseJson;
        }

        return null;
    }

    // Write one of these types
    public void write( IJsonWriter w, object val ) {
        w.writeDictionary( () => {
            var writing = val as IJsonWriting;
            writing?.onJsonWriting( w );

            foreach ( var jmi in members.Where( x => !x.deprecated ) ) {
                w.writeKeyNoEscaping( jmi.jsonKey );
                w.writeValue( jmi.getValue( val ) );
            }

            var written = val as IJsonWritten;
            written?.onJsonWritten( w );
        } );
    }

    // Read one of these types.
    // NB: Although PetaJson.JsonParseInto only works on reference type, when using reflection
    //     it also works for value types so we use the one method for both
    public void parseInto( IJsonReader r, object into ) {
        var loading = into as IJsonLoading;
        loading?.onJsonLoading( r );

        r.parseDictionary( key => { parseFieldOrProperty( r, into, key ); } );

        var loaded = into as IJsonLoaded;
        loaded?.onJsonLoaded( r );
    }

    // The member info is stored in a list (as opposed to a dictionary) so that
    // the json is written in the same order as the fields/properties are defined
    // On loading, we assume the fields will be in the same order, but need to
    // handle if they're not.  This function performs a linear search, but
    // starts after the last found item as an optimization that should work
    // most of the time.
    int lastFoundIndex = 0;

    bool findMemberInfo( string name, out JsonMemberInfo found ) {
        for ( int i = 0; i < members.Count; i++ ) {
            int index = ( i + lastFoundIndex ) % members.Count;
            var jmi = members[index];
            if ( jmi.jsonKey != name ) {
                continue;
            }

            lastFoundIndex = index;
            found = jmi;
            return true;
        }

        found = null;
        return false;
    }

    // Parse a value from IJsonReader into an object instance
    public void parseFieldOrProperty( IJsonReader r, object into, string key ) {
        // IJsonLoadField
        if ( into is IJsonLoadField lf && lf.onJsonField( r, key ) ) {
            return;
        }

        // Find member
        JsonMemberInfo jmi;
        if ( !findMemberInfo( key, out jmi ) ) {
            return;
        }

        // Try to keep existing instance
        if ( jmi.keepInstance ) {
            var subInto = jmi.getValue( @into );
            if ( subInto != null ) {
                r.parseInto( subInto );
                return;
            }
        }

        // Parse and set
        var val = r.parse( jmi.memberType );
        jmi.setValue( @into, val );
    }

    // Get the reflection info for a specified type
    public static ReflectionInfo getReflectionInfo( Type type ) {
        // Check cache
        return CACHE.get( type, () => {
            var allMembers = Utils.getAllFieldsAndProperties( type );

            // Does type have a [Json] attribute
            bool typeMarked = type.GetCustomAttributes( typeof(JsonAttribute), true ).OfType<JsonAttribute>().Any();

            // Do any members have a [Json] attribute
            bool anyFieldsMarked =
                allMembers.Any(
                    x => x.GetCustomAttributes( typeof(JsonAttribute), false ).OfType<JsonAttribute>().Any() );
            {
                // Should we serialize all public methods?
                bool serializeAllPublics = typeMarked || !anyFieldsMarked;

                // Build 
                var ri = createReflectionInfo( type, mi => {
                    // Explicitly excluded?
                    if ( mi.GetCustomAttributes( typeof(JsonExcludeAttribute), false ).Any() ) {
                        return null;
                    }

                    // Get attributes
                    var attr = mi.GetCustomAttributes( typeof(JsonAttribute), false ).OfType<JsonAttribute>()
                                 .FirstOrDefault();
                    if ( attr != null ) {
                        return new JsonMemberInfo() {
                            member = mi,
                            jsonKey = attr.key ?? ( mi.Name.Substring( 0, 1 ).ToLower() + mi.Name.Substring( 1 ) ),
                            keepInstance = attr.keepInstance,
                            deprecated = attr.deprecated,
                        };
                    }

                    // Serialize all publics?
                    if ( serializeAllPublics && Utils.isPublic( mi ) ) {
                        return new JsonMemberInfo() {
                            member = mi,
                            jsonKey = mi.Name.Substring( 0, 1 ).ToLower() + mi.Name.Substring( 1 ),
                        };
                    }

                    return null;
                } );
                return ri;
            }
        } );
    }

    public static ReflectionInfo createReflectionInfo( Type type, Func<MemberInfo, JsonMemberInfo> callback ) {
        // Work out properties and fields
        var members = Utils.getAllFieldsAndProperties( type ).Select( callback ).Where( x => x != null )
                           .ToList();

        // Anything with KeepInstance must be a reference type
        var invalid = members.FirstOrDefault( x => x.keepInstance && x.memberType.IsValueType );
        if ( invalid != null ) {
            throw new InvalidOperationException(
                "KeepInstance=true can only be applied to reference types ("
                + type.FullName + "." + invalid.member + ")" );
        }

        // Must have some members
        if ( !members.Any() &&
             !Attribute.IsDefined( type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false ) ) {
            return null;
        }

        // Create reflection info
        return new ReflectionInfo() {members = members};
    }
}

public class ThreadSafeCache<TKey, TValue> {
    public ThreadSafeCache() {
    }

    public TValue get( TKey key, Func<TValue> createIt ) {
        // Check if already exists
        _lock.EnterReadLock();
        try {
            TValue val;
            if ( map.TryGetValue( key, out val ) ) {
                return val;
            }
        } finally {
            _lock.ExitReadLock();
        }

        // Nope, take lock and try again
        _lock.EnterWriteLock();
        try {
            // Check again before creating it
            TValue val;
            if ( map.TryGetValue( key, out val ) ) {
                return val;
            }

            // Store the new one
            val = createIt();
            map[key] = val;

            return val;
        } finally {
            _lock.ExitWriteLock();
        }
    }

    public bool tryGetValue( TKey key, out TValue val ) {
        _lock.EnterReadLock();
        try {
            return map.TryGetValue( key, out val );
        } finally {
            _lock.ExitReadLock();
        }
    }

    public void set( TKey key, TValue value ) {
        _lock.EnterWriteLock();
        try {
            map[key] = value;
        } finally {
            _lock.ExitWriteLock();
        }
    }

    readonly Dictionary<TKey, TValue> map = new Dictionary<TKey, TValue>();
    readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
}

internal static class Utils {
    // Get all fields and properties of a type
    public static IEnumerable<MemberInfo> getAllFieldsAndProperties( Type t ) {
        if ( t == null ) {
            return Enumerable.Empty<FieldInfo>();
        }

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                             BindingFlags.DeclaredOnly;
        return t.GetMembers( flags ).Where( x => x is FieldInfo || x is PropertyInfo )
                .Concat( getAllFieldsAndProperties( t.BaseType ) );
    }

    public static Type findGenericInterface( Type type, Type tItf ) {
        foreach ( var t in type.GetInterfaces() ) {
            // Is this a generic list?
            if ( t.IsGenericType && t.GetGenericTypeDefinition() == tItf ) {
                return t;
            }
        }

        return null;
    }

    public static bool isPublic( MemberInfo mi ) {
        // Public field
        var fi = mi as FieldInfo;
        if ( fi != null ) {
            return fi.IsPublic;
        }

        // Public property
        // (We only check the get method so we can work with anonymous types)
        var pi = mi as PropertyInfo;
        if ( pi != null ) {
            var gm = pi.GetGetMethod( true );
            return ( gm != null && gm.IsPublic );
        }

        return false;
    }

    public static Type resolveInterfaceToClass( Type tItf ) {
        // Generic type
        if ( tItf.IsGenericType ) {
            var genDef = tItf.GetGenericTypeDefinition();

            // IList<> -> List<>
            if ( genDef == typeof(IList<>) ) {
                return typeof(List<>).MakeGenericType( tItf.GetGenericArguments() );
            }

            // IDictionary<string,> -> Dictionary<string,>
            if ( genDef == typeof(IDictionary<,>) && tItf.GetGenericArguments()[0] == typeof(string) ) {
                return typeof(Dictionary<,>).MakeGenericType( tItf.GetGenericArguments() );
            }
        }

        // IEnumerable -> List<object>
        if ( tItf == typeof(IEnumerable) ) {
            return typeof(List<object>);
        }

        // IDictionary -> Dictionary<string,object>
        return ( tItf == typeof(IDictionary) )
            ? typeof(Dictionary<string, object>)
            : tItf;
    }

    public static long toUnixMilliseconds( DateTime This ) {
        return (long) This.Subtract( new DateTime( 1970, 1, 1 ) ).TotalMilliseconds;
    }

    public static DateTime fromUnixMilliseconds( long timeStamp ) {
        return new DateTime( 1970, 1, 1 ).AddMilliseconds( timeStamp );
    }
}

public class Tokenizer {
    public Tokenizer( TextReader r, JsonOptions options ) {
        underlying = r;
        this.options = options;
        fillBuffer();
        nextChar();
        nextToken();
    }

    private readonly JsonOptions options;
    private readonly StringBuilder sb = new StringBuilder();
    private readonly TextReader underlying;
    private readonly char[] buf = new char[4096];
    private int pos;
    private int bufUsed;
    private StringBuilder rewindBuffer;
    private int rewindBufferPos;
    private JsonLineOffset currentCharPos;
    private char currentChar;
    private readonly Stack<ReaderState> bookmarks = new Stack<ReaderState>();

    public JsonLineOffset currentTokenPosition;
    public Token currentToken;
    public LiteralKind literalKind;
    public string String;

    public object literalValue {
        get {
            if ( currentToken != Token.Literal ) {
                throw new InvalidOperationException( "token is not a literal" );
            }

            switch ( literalKind ) {
                case LiteralKind.Null: return null;
                case LiteralKind.False: return false;
                case LiteralKind.True: return true;
                case LiteralKind.String: return String;
                case LiteralKind.SignedInteger: return long.Parse( String, CultureInfo.InvariantCulture );
                case LiteralKind.UnsignedInteger:
                    if ( String.StartsWith( "0x" ) || String.StartsWith( "0X" ) ) {
                        return Convert.ToUInt64( String.Substring( 2 ), 16 );
                    } else {
                        return ulong.Parse( String, CultureInfo.InvariantCulture );
                    }
                case LiteralKind.FloatingPoint: return double.Parse( String, CultureInfo.InvariantCulture );
            }

            return null;
        }
    }

    public Type literalType {
        get {
            if ( currentToken != Token.Literal ) {
                throw new InvalidOperationException( "token is not a literal" );
            }

            switch ( literalKind ) {
                case LiteralKind.Null: return typeof(object);
                case LiteralKind.False: return typeof(bool);
                case LiteralKind.True: return typeof(bool);
                case LiteralKind.String: return typeof(string);
                case LiteralKind.SignedInteger: return typeof(long);
                case LiteralKind.UnsignedInteger: return typeof(ulong);
                case LiteralKind.FloatingPoint: return typeof(double);
            }

            return null;
        }
    }

    // This object represents the entire state of the reader and is used for rewind
    struct ReaderState {
        public ReaderState( Tokenizer tokenizer ) {
            currentCharPos = tokenizer.currentCharPos;
            currentChar = tokenizer.currentChar;
            _string = tokenizer.String;
            literalKind = tokenizer.literalKind;
            rewindBufferPos = tokenizer.rewindBufferPos;
            currentTokenPos = tokenizer.currentTokenPosition;
            currentToken = tokenizer.currentToken;
        }

        public void apply( Tokenizer tokenizer ) {
            tokenizer.currentCharPos = currentCharPos;
            tokenizer.currentChar = currentChar;
            tokenizer.rewindBufferPos = rewindBufferPos;
            tokenizer.currentToken = currentToken;
            tokenizer.currentTokenPosition = currentTokenPos;
            tokenizer.String = _string;
            tokenizer.literalKind = literalKind;
        }

        private readonly JsonLineOffset currentCharPos;
        private readonly JsonLineOffset currentTokenPos;
        private readonly char currentChar;
        private readonly Token currentToken;
        private readonly LiteralKind literalKind;
        private readonly string _string;
        private readonly int rewindBufferPos;
    }

    // Create a rewind bookmark
    public void createBookmark() {
        bookmarks.Push( new ReaderState( this ) );
        if ( rewindBuffer == null ) {
            rewindBuffer = new StringBuilder();
            rewindBufferPos = 0;
        }
    }

    // Discard bookmark
    public void discardBookmark() {
        bookmarks.Pop();
        if ( bookmarks.Count == 0 ) {
            rewindBuffer = null;
            rewindBufferPos = 0;
        }
    }

    // Rewind to a bookmark
    public void rewindToBookmark() {
        bookmarks.Pop().apply( this );
    }

    // Fill buffer by reading from underlying TextReader
    void fillBuffer() {
        bufUsed = underlying.Read( buf, 0, buf.Length );
        pos = 0;
    }

    // Get the next character from the input stream
    // (this function could be extracted into a few different methods, but is mostly inlined
    //  for performance - yes it makes a difference)
    public char nextChar() {
        if ( rewindBuffer == null ) {
            if ( pos >= bufUsed ) {
                if ( bufUsed > 0 ) {
                    fillBuffer();
                }

                if ( bufUsed == 0 ) {
                    return currentChar = '\0';
                }
            }

            // Next
            currentCharPos.offset++;
            return currentChar = buf[pos++];
        }

        if ( rewindBufferPos < rewindBuffer.Length ) {
            currentCharPos.offset++;
            return currentChar = rewindBuffer[rewindBufferPos++];
        } else {
            if ( pos >= bufUsed && bufUsed > 0 ) {
                fillBuffer();
            }

            currentChar = bufUsed == 0 ? '\0' : buf[pos++];
            rewindBuffer.Append( currentChar );
            rewindBufferPos++;
            currentCharPos.offset++;
            return currentChar;
        }
    }

    // Read the next token from the input stream
    // (Mostly inline for performance)
    public void nextToken() {
        while ( true ) {
            // Skip whitespace and handle line numbers
            while ( true ) {
                if ( currentChar == '\r' ) {
                    if ( nextChar() == '\n' ) {
                        nextChar();
                    }

                    currentCharPos.line++;
                    currentCharPos.offset = 0;
                } else if ( currentChar == '\n' ) {
                    if ( nextChar() == '\r' ) {
                        nextChar();
                    }

                    currentCharPos.line++;
                    currentCharPos.offset = 0;
                } else if ( currentChar == ' ' ) {
                    nextChar();
                } else if ( currentChar == '\t' ) {
                    nextChar();
                } else {
                    break;
                }
            }

            // Remember position of token
            currentTokenPosition = currentCharPos;

            // Handle common characters first
            switch ( currentChar ) {
                case '/':
                    // Comments not support in strict mode
                    if ( ( options & JsonOptions.StrictParser ) != 0 ) {
                        throw new InvalidDataException( "syntax error, unexpected character '" + currentChar + "'" );
                    }

                    // Process comment
                    nextChar();
                    switch ( currentChar ) {
                        case '/':
                            nextChar();
                            while ( currentChar != '\0' && currentChar != '\r' && currentChar != '\n' ) {
                                nextChar();
                            }

                            break;

                        case '*':
                            bool endFound = false;
                            while ( !endFound && currentChar != '\0' ) {
                                if ( currentChar == '*' ) {
                                    nextChar();
                                    if ( currentChar == '/' ) {
                                        endFound = true;
                                    }
                                }

                                nextChar();
                            }

                            break;

                        default:
                            throw new InvalidDataException( "syntax error, unexpected character after slash" );
                    }

                    continue;

                case '\"':
                case '\'': {
                    sb.Length = 0;
                    var quoteKind = currentChar;
                    nextChar();
                    while ( currentChar != '\0' ) {
                        if ( currentChar == '\\' ) {
                            nextChar();
                            var escape = currentChar;
                            switch ( escape ) {
                                case '\"':
                                    sb.Append( '\"' );
                                    break;
                                case '\\':
                                    sb.Append( '\\' );
                                    break;
                                case '/':
                                    sb.Append( '/' );
                                    break;
                                case 'b':
                                    sb.Append( '\b' );
                                    break;
                                case 'f':
                                    sb.Append( '\f' );
                                    break;
                                case 'n':
                                    sb.Append( '\n' );
                                    break;
                                case 'r':
                                    sb.Append( '\r' );
                                    break;
                                case 't':
                                    sb.Append( '\t' );
                                    break;
                                case 'u':
                                    var sbHex = new StringBuilder();
                                    for ( int i = 0; i < 4; i++ ) {
                                        nextChar();
                                        sbHex.Append( currentChar );
                                    }

                                    sb.Append( (char) Convert.ToUInt16( sbHex.ToString(), 16 ) );
                                    break;

                                default:
                                    throw new InvalidDataException(
                                        "Invalid escape sequence in string literal: '\\" + currentChar + "'" );
                            }
                        } else if ( currentChar == quoteKind ) {
                            String = sb.ToString();
                            currentToken = Token.Literal;
                            literalKind = LiteralKind.String;
                            nextChar();
                            return;
                        } else {
                            sb.Append( currentChar );
                        }

                        nextChar();
                    }

                    throw new InvalidDataException( "syntax error, unterminated string literal" );
                }

                case '{':
                    currentToken = Token.OpenBrace;
                    nextChar();
                    return;
                case '}':
                    currentToken = Token.CloseBrace;
                    nextChar();
                    return;
                case '[':
                    currentToken = Token.OpenSquare;
                    nextChar();
                    return;
                case ']':
                    currentToken = Token.CloseSquare;
                    nextChar();
                    return;
                case '=':
                    currentToken = Token.Equal;
                    nextChar();
                    return;
                case ':':
                    currentToken = Token.Colon;
                    nextChar();
                    return;
                case ';':
                    currentToken = Token.SemiColon;
                    nextChar();
                    return;
                case ',':
                    currentToken = Token.Comma;
                    nextChar();
                    return;
                case '\0':
                    currentToken = Token.Eof;
                    return;
            }

            // Number?
            if ( char.IsDigit( currentChar ) || currentChar == '-' ) {
                tokenizeNumber();
                return;
            }

            // Identifier?  (checked for after everything else as identifiers are actually quite rare in valid json)
            if ( char.IsLetter( currentChar ) || currentChar == '_' || currentChar == '$' ) {
                // Find end of identifier
                sb.Length = 0;
                while ( char.IsLetterOrDigit( currentChar ) || currentChar == '_' || currentChar == '$' ) {
                    sb.Append( currentChar );
                    nextChar();
                }

                String = sb.ToString();

                // Handle special identifiers
                switch ( String ) {
                    case "true":
                        literalKind = LiteralKind.True;
                        currentToken = Token.Literal;
                        return;

                    case "false":
                        literalKind = LiteralKind.False;
                        currentToken = Token.Literal;
                        return;

                    case "null":
                        literalKind = LiteralKind.Null;
                        currentToken = Token.Literal;
                        return;
                }

                currentToken = Token.Identifier;
                return;
            }

            // What the?
            throw new InvalidDataException( "syntax error, unexpected character '" + currentChar + "'" );
        }
    }

    // Parse a sequence of characters that could make up a valid number
    // For performance, we don't actually parse it into a number yet.  When using PetaJsonEmit we parse
    // later, directly into a value type to avoid boxing
    private void tokenizeNumber() {
        sb.Length = 0;

        // Leading negative sign
        bool signed = false;
        if ( currentChar == '-' ) {
            signed = true;
            sb.Append( currentChar );
            nextChar();
        }

        // Hex prefix?
        bool hex = false;
        if ( currentChar == '0' && ( options & JsonOptions.StrictParser ) == 0 ) {
            sb.Append( currentChar );
            nextChar();
            if ( currentChar == 'x' || currentChar == 'X' ) {
                sb.Append( currentChar );
                nextChar();
                hex = true;
            }
        }

        // Process characters, but vaguely figure out what type it is
        bool cont = true;
        bool fp = false;
        while ( cont ) {
            switch ( currentChar ) {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    sb.Append( currentChar );
                    nextChar();
                    break;

                case 'A':
                case 'a':
                case 'B':
                case 'b':
                case 'C':
                case 'c':
                case 'D':
                case 'd':
                case 'F':
                case 'f':
                    if ( !hex ) {
                        cont = false;
                    } else {
                        sb.Append( currentChar );
                        nextChar();
                    }

                    break;

                case '.':
                    if ( hex ) {
                        cont = false;
                    } else {
                        fp = true;
                        sb.Append( currentChar );
                        nextChar();
                    }

                    break;

                case 'E':
                case 'e':
                    if ( !hex ) {
                        fp = true;
                        sb.Append( currentChar );
                        nextChar();
                        if ( currentChar == '+' || currentChar == '-' ) {
                            sb.Append( currentChar );
                            nextChar();
                        }
                    }

                    break;

                default:
                    cont = false;
                    break;
            }
        }

        if ( char.IsLetter( currentChar ) ) {
            throw new InvalidDataException( "syntax error, invalid character following number '" + sb + "'" );
        }

        // Setup token
        String = sb.ToString();
        currentToken = Token.Literal;

        // Setup literal kind
        if ( fp ) {
            literalKind = LiteralKind.FloatingPoint;
        } else if ( signed ) {
            literalKind = LiteralKind.SignedInteger;
        } else {
            literalKind = LiteralKind.UnsignedInteger;
        }
    }

    // Check the current token, throw exception if mismatch
    public void check( Token tokenRequired ) {
        if ( tokenRequired != currentToken ) {
            throw new InvalidDataException(
                "syntax error, expected " + tokenRequired + " found " + currentToken );
        }
    }

    // Skip token which must match
    public void skip( Token tokenRequired ) {
        check( tokenRequired );
        nextToken();
    }

    // Skip token if it matches
    public bool skipIf( Token tokenRequired ) {
        if ( tokenRequired != currentToken ) {
            return false;
        }

        nextToken();
        return true;
    }
}

}

}