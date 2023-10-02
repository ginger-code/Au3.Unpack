namespace Au3.Unpack.Collections

open System

module private Bits =
    let byteToBits (byte: byte) =
        let result = Array.zeroCreate 8
        let mutable byte = byte

        for i in 0..7 do
            result.[7 - i] <- byte &&& 1uy
            byte <- byte >>> 1

        result

    let bytesToBits (bytes: byte Span) =
        let result = Array.zeroCreate (bytes.Length * 8)

        for i in 0 .. bytes.Length - 1 do
            let byte = byteToBits bytes.[i]
            let offset = i * 8

            for j in 0..7 do
                result.[offset + j] <- byte.[j]

        result

type BitStream private (data: byte array, offset: int ref) =
    new(data: byte Span) = BitStream(Bits.bytesToBits data, ref 0)

    member this.GetBits num =
        if num + offset.Value > data.Length then
            failwith
                $"Could not read {num} bits from stream with remaining length {data.Length - offset.Value}"

        let mutable result = 0

        for i in 0 .. num - 1 do
            result <- result <<< 1 ||| int data[i + offset.Value]

        offset.Value <- num + offset.Value

        result

    member this.GetByte() = this.GetBits 8 |> byte
