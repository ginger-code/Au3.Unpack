module Au3.Unpack.PRNG.LAME

open System
open System.Buffers.Binary

open Au3.Unpack


/// Implementation of the LAME algorithm used for EA06 decryption
/// Generates an infinite series of values for decryption which can be enumerated as needed
let lame (seed : int) =
    // Initialize variables
    let mutable x = 0
    let mutable y = 0
    let mutable data : uint array = Array.zeroCreate 17
    let mutable seed = uint seed

    let shuffle () =
        let rolled =
            (wrappingRotateLeft data[x] 9)
            + (wrappingRotateLeft data[y] 13)

        data[x] <- rolled

        x <-
            match x with
            | 0 -> 16
            | _ -> x - 1

        y <-
            match y with
            | 0 -> 16
            | _ -> y - 1

        let lo = rolled <<< 20
        let hi = 0x3ff00000u ||| (rolled >>> 12)
        let comb = (uint64 hi <<< 32) ||| (uint64 lo)

        let dub =
            BitConverter.GetBytes comb
            |> BinaryPrimitives.ReadDoubleLittleEndian

        dub - 1.0

    // Randomize

    for i in 0..16 do
        seed <- seed * 0x53A9B4FBu
        seed <- 1u - seed
        data.[i] <- seed

    x <- 0
    y <- 10

    for _ in 0..8 do
        shuffle () |> ignore

    // Generate values
    let next () =
        let _ = shuffle ()
        let x = shuffle () * 256.0

        if int x < 256 then
            byte x
        else
            255uy

    Seq.initInfinite (ignore >> next)

let lameN seed n = lame seed |> Seq.take n |> Array.ofSeq
