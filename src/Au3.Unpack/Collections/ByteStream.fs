namespace Au3.Unpack.Collections

open System
open System.Buffers.Binary


type ByteStream =

    private
        {
            Offset : int ref
            Data : byte Memory
        }

    member private this.GetBytesInner(length, advanceOffset) : byte Memory =
        let slice = this.Data.Slice (this.Offset.Value, length)

        if advanceOffset then
            this.Offset.Value <- this.Offset.Value + length

        slice

    member this.PeekNextByte() =
        if this.Offset.Value < this.Data.Length then
            ValueSome <| this.Data.Span.[this.Offset.Value]
        else
            ValueNone

    member this.OffsetInBounds(offset : uint) = this.Data.Length > int offset
    member this.Read n = this.GetBytesInner (n, true)

    member this.ReadRemaining() =
        let remaining = this.Data.Length - this.Offset.Value
        let slice = this.Data.Slice (this.Offset.Value, remaining)
        this.Offset.Value <- this.Data.Length
        slice

    member this.SkipBytes n =
        let _ = this.GetBytesInner (n, true)
        ()

    member this.CanRead() = this.Offset.Value + 32 < this.Data.Length

    member this.ReadU8() = this.GetBytesInner(1, true).Span.[0]

    member this.ReadU16() =
        BinaryPrimitives.ReadUInt16LittleEndian (
            this.GetBytesInner(2, true).Span
        )

    member this.ReadU32() =
        BinaryPrimitives.ReadUInt32LittleEndian (
            this.GetBytesInner(4, true).Span
        )

    member this.ReadU32BE() =
        BinaryPrimitives.ReadUInt32BigEndian (this.GetBytesInner(4, true).Span)

    member this.ReadU64() =
        BinaryPrimitives.ReadUInt64LittleEndian (
            this.GetBytesInner(8, true).Span
        )

    member this.ReadI16() =
        BinaryPrimitives.ReadInt16LittleEndian (
            this.GetBytesInner(2, true).Span
        )

    member this.ReadI32() =
        BinaryPrimitives.ReadInt32LittleEndian (
            this.GetBytesInner(4, true).Span
        )

    member this.ReadI64() =
        BinaryPrimitives.ReadInt64LittleEndian (
            this.GetBytesInner(8, true).Span
        )

    member this.ReadF32() =
        BinaryPrimitives.ReadSingleLittleEndian (
            this.GetBytesInner(4, true).Span
        )

    member this.ReadF64() =
        BinaryPrimitives.ReadDoubleLittleEndian (
            this.GetBytesInner(8, true).Span
        )

[<RequireQualifiedAccess>]
module ByteStream =
    let createFrom (data : byte Memory) : ByteStream =
        {
            Offset = ref 0
            Data = data
        }
