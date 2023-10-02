namespace Au3.Unpack

open System
open System.Buffers.Binary
open System.Text

open Au3.Unpack.Collections

module Scanning =

    let private readString
        (encryptionMethod : EncryptionMethod)
        (key1, key2)
        (stream : ByteStream)
        =
        try
            let length = (stream.ReadU32 () |> int) ^^^ key1
            let encodingKey = length + key2
            let isUnicode = EncryptionMethod.isUnicode encryptionMethod

            let length =
                if isUnicode then
                    length <<< 1
                else
                    length

            let bytes = stream.Read length
            Decryption.decryptInPlace encryptionMethod encodingKey bytes.Span

            if isUnicode then
                Encoding.Unicode.GetString bytes.Span
            else
                Encoding.UTF8.GetString bytes.Span
        with _ ->
            failwith "Failed to read string"

    let internal readDate (stream : ByteStream) =
        let high = (stream.ReadU32 () |> int64) <<< 32
        let low = stream.ReadU32 () |> int64
        let fileTime = high ||| low

        try
            DateTime.FromFileTime fileTime
        with a ->
            failwith $"Failed to read file time from '{fileTime}'"


    let private readFileMagic
        (encryptionMethod : EncryptionMethod)
        (stream : ByteStream)
        =
        let magic =
            let magic = EncryptionMethod.resourceTypeMagic encryptionMethod
            let key = stream.Read 4
            Decryption.decryptInPlace encryptionMethod magic key.Span
            key.Span

        if magic.SequenceEqual "FILE"B then
            BinaryPrimitives.ReadUInt32LittleEndian magic
            |> Some
        else
            None

    let internal readSubtype
        (encryptionMethod : EncryptionMethod)
        (stream : ByteStream)
        =
        readString
            encryptionMethod
            (EncryptionMethod.subResourceMagic encryptionMethod)
            stream

    let private readResourcePath
        (encryptionMethod : EncryptionMethod)
        (stream : ByteStream)
        =
        readString
            encryptionMethod
            (EncryptionMethod.resourcePathMagic encryptionMethod)
            stream

    let private trySkipNoCmdExecute
        (encryptionMethod : EncryptionMethod)
        (subType : string)
        (stream : ByteStream)
        =
        if subType = ">>>AUTOIT NO CMDEXECUTE<<<" then
            stream.SkipBytes 1

            let next =
                0x18u
                + (stream.ReadU32 ()
                   ^^^ (EncryptionMethod.resourceSizeMagic encryptionMethod))

            stream.SkipBytes (int next)
            false
        else
            true

    let private parseHeaderAndData
        (encryptionMethod : EncryptionMethod)
        (stream : ByteStream)
        : Au3Resource' option =
        let fileMagic = readFileMagic encryptionMethod stream

        match fileMagic with
        | None -> None
        | Some fileMagic ->
            let subType = readSubtype encryptionMethod stream
            let path = readResourcePath encryptionMethod stream

            if trySkipNoCmdExecute encryptionMethod subType stream then
                let isCompressed = stream.ReadU8 () = 1uy

                let compressedSize =
                    stream.ReadU32 ()
                    ^^^ EncryptionMethod.resourceSizeMagic encryptionMethod
                    |> int

                let decompressedSize =
                    stream.ReadU32 ()
                    ^^^ EncryptionMethod.resourceSizeMagic encryptionMethod
                    |> int

                let crc =
                    stream.ReadU32 ()
                    ^^^ EncryptionMethod.resourceCrcMagic encryptionMethod

                let creationDate = readDate stream
                let modificationDate = readDate stream
                let content = stream.Read compressedSize
                let key = EncryptionMethod.resourceContentMagic encryptionMethod
                Decryption.decryptInPlace encryptionMethod key content.Span

                if not (crcMatches crc content.Span) then
                    failwith "CRC mismatch"

                let header =
                    {
                        Magic = int fileMagic
                        SubType = subType
                        Path = path
                        IsCompressed = isCompressed
                        CompressedSize = int compressedSize
                        DecompressedSize = int decompressedSize
                        Crc = int crc
                        CreationDate = creationDate
                        ModificationDate = modificationDate
                    }

                let compression =
                    if isCompressed then
                        Compressed
                    else
                        Decompressed

                let compilation (data : byte Memory) =
                    if subType = ">>>AUTOIT SCRIPT<<<" then
                        Compiled data
                    elif subType = ">AUTOIT UNICODE SCRIPT<" then
                        Encoding.Unicode.GetString data.Span |> Text
                    elif subType = ">AUTOIT SCRIPT<" then
                        Encoding.UTF8.GetString data.Span |> Text
                    else
                        Raw data

                let data = content |> compilation |> compression

                Some
                    {
                        Header = header
                        Data = data
                    }
            else
                None

    let internal parseAllResources
        (encryptionMethod : EncryptionMethod)
        (data : byte Memory)
        =
        let resources = ResizeArray ()

        let mutable stream = ByteStream.createFrom data
        let mutable broke = false
        let _checksumData = stream.Read 16

        while not broke && stream.CanRead () do
            match parseHeaderAndData encryptionMethod stream with
            | Some resource -> resources.Add resource
            | None -> ()

        resources.ToArray ()
