﻿using Neat.Map.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedCorners.Forms.GoogleMaps
{
    public sealed class Geocoder
    {
        internal static Func<string, Task<IEnumerable<Position>>> GetPositionsForAddressAsyncFunc = null;

        internal static Func<Position, Task<IEnumerable<string>>> GetAddressesForPositionFuncAsync = null;

        public Task<IEnumerable<string>> GetAddressesForPositionAsync(Position position)
        {
            if (GetAddressesForPositionFuncAsync == null)
                throw new InvalidOperationException("You MUST call RedCorners.Forms.GoogleMapsSystem.Init (); prior to using it.");
            return GetAddressesForPositionFuncAsync(position);
        }

        public Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address)
        {
            if (GetPositionsForAddressAsyncFunc == null)
                throw new InvalidOperationException("You MUST call RedCorners.Forms.GoogleMapsSystem.Init (); prior to using it.");
            return GetPositionsForAddressAsyncFunc(address);
        }
    }
}