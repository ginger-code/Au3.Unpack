namespace Au3.Unpack

open System
open System.Runtime.CompilerServices

[<Struct ; IsReadOnly>]
type Au3Header =
    {
        Magic : int
        SubType : string
        Path : string
        IsCompressed : bool
        CompressedSize : int
        DecompressedSize : int
        Crc : int
        CreationDate : DateTime
        ModificationDate : DateTime
    }

[<Struct ; IsReadOnly>]
type internal Au3ResourceCompilation =
    | Compiled of compiledData : byte Memory
    | Raw of rawData : byte Memory
    | Text of textData : string

[<Struct ; IsReadOnly>]
type internal Au3ResourceData' =
    | Decompressed of decompressed : Au3ResourceCompilation
    | Compressed of compressed : Au3ResourceCompilation

[<Struct ; IsReadOnly>]
type internal Au3Resource' =
    {
        Header : Au3Header
        Data : Au3ResourceData'
    }

[<Struct ; IsReadOnly>]
type internal Au3Resource'' =
    {
        Header : Au3Header
        Data : Au3ResourceCompilation
    }

[<Struct ; IsReadOnly>]
type Au3ResourceData =
    | Binary of data : byte Memory
    | Script of text : string

[<Struct ; IsReadOnly>]
type Au3Resource =
    {
        Header : Au3Header
        Data : Au3ResourceData
    }
