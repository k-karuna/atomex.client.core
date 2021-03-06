﻿using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Atomex.Blockchain.Ethereum.ERC20
{
    [Event("Added")]
    public class ERC20AddedEventDTO : IEventDTO
    {
        [Parameter("bytes32", "_hashedSecret", 1, true)]
        public byte[] HashedSecret { get; set; }

        [Parameter("address", "_sender", 2, false)]
        public string Initiator { get; set; }

        [Parameter("uint256", "_value", 3, false)]
        public BigInteger Value { get; set; }
    }
}