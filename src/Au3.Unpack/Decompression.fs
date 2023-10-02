module Au3.Unpack.Decompression

open System

open Au3.Unpack.Collections
open Au3.Unpack

module Decompression =
    [<Literal>]
    let internal MaxScriptSize = 10_000_000

    let internal numberOfBytesToCopy (stream : BitStream) =
        let sizePlus = 0x0
        let size = stream.GetBits 2

        if size = 3 then
            let sizePlus = 0x3
            let size = stream.GetBits 3

            if size = 7 then
                let sizePlus = 0xA
                let size = stream.GetBits 5

                if size = 0x1F then
                    let sizePlus = 0x29
                    let size = stream.GetBits 8

                    if size = 0xFF then
                        let mutable sizePlus = 0x128
                        let mutable size = stream.GetBits 8

                        while size = 0xFF do
                            sizePlus <- sizePlus + 0xFF
                            size <- stream.GetBits 8

                        size + sizePlus + 3
                    else
                        size + sizePlus + 3
                else
                    size + sizePlus + 3
            else
                size + sizePlus + 3
        else
            size + sizePlus + 3

    let internal getOffsetAndLength encryptionMethod (binary : BitStream) =
        match encryptionMethod with
        | EA06 ->
            let offset = binary.GetBits 15
            let length = numberOfBytesToCopy binary
            offset, length

    let internal decompressData
        (encryptionMethod : EncryptionMethod)
        (decompressedSize : int)
        (dataSpan : byte Span)
        =
        let mutable binary = BitStream dataSpan
        let buffer = Array.zeroCreate decompressedSize
        let bufferSpan = buffer.AsSpan ()
        let mutable position = 0
        let encryptionFlag = EncryptionMethod.versionFlag encryptionMethod

        while position < decompressedSize do
            if binary.GetBits 1 = encryptionFlag then
                bufferSpan.[position] <- binary.GetByte ()
                position <- position + 1

            else
                let offset, length = getOffsetAndLength encryptionMethod binary

                for _ = length downto 1 do
                    bufferSpan.[position] <- bufferSpan.[position - offset]
                    position <- position + 1

        buffer.AsMemory ()

    let decompress (data : byte Memory) =
        let mutable stream = ByteStream.createFrom data
        let magic = stream.Read 4
        let encryptionMethod = magic.ToArray () |> EncryptionMethod.fromMagic
        let decompressedSize = stream.ReadU32BE () |> int

        if decompressedSize > MaxScriptSize then
            failwith "Script size too large to decompress"

        let data = stream.ReadRemaining().Span
        decompressData encryptionMethod decompressedSize data

    let internal decompressResourceData
        (header : Au3Resource')
        : Au3Resource'' =
        {
            Header = header.Header
            Data =
                match header.Data with
                | Decompressed decompressed -> decompressed
                | Compressed compressed ->
                    match compressed with
                    | Compiled compiledData ->
                        decompress compiledData |> Compiled
                    | Raw rawData -> decompress rawData |> Raw
                    | Text textData -> textData |> Text
        }
