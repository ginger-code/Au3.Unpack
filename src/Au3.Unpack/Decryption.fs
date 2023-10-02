module Au3.Unpack.Decryption

open System

open Au3.Unpack
open Au3.Unpack.PRNG.LAME

let internal ea06Decrypt (data : byte Span) (seed : int) =
    let key = lame seed |> Seq.take data.Length |> Array.ofSeq
    xor data (key.AsSpan ())

let internal ea06DecryptInPlace (data : byte Span) (seed : int) =
    let keySeq = lame seed
    let keyEnum = keySeq.GetEnumerator ()

    for i in 0 .. data.Length - 1 do
        let key =
            keyEnum.MoveNext () |> ignore
            keyEnum.Current

        data.[i] <- data.[i] ^^^ key

let decrypt (method : EncryptionMethod) key (data : byte Span) =
    match method with
    | EA06 -> ea06Decrypt data key

let decryptInPlace (method : EncryptionMethod) key (data : byte Span) =
    match method with
    | EA06 -> ea06DecryptInPlace data key
