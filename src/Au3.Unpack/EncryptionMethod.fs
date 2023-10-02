namespace Au3.Unpack

open System

type EncryptionMethod = EA06

[<RequireQualifiedAccess>]
module EncryptionMethod =
    let isUnicode =
        function
        | EA06 -> true

    let versionFlag =
        function
        | EA06 -> 1

    let versionMagic = "EA06"B

    let resourceTypeMagic =
        function
        | EA06 -> 0x18EE

    let subResourceMagic =
        function
        | EA06 -> 0xADBC, 0xB33F

    let resourcePathMagic =
        function
        | EA06 -> 0xF820, 0xF479

    let resourceSizeMagic =
        function
        | EA06 -> 0x87BCu

    let resourceCrcMagic =
        function
        | EA06 -> 0xA685u

    let resourceContentMagic =
        function
        | EA06 -> 0x2477

    let fromMagic =
        function
        | "EA06"B -> EA06
        | x ->
            failwith
                $"Unrecognized encryption method: {String.decodeUnicode (x.AsSpan ())}"
