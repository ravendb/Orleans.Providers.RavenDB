﻿using Orleans;

namespace UnitTests.Grains
{
    public interface IPlayerGrain : IGrainWithIntegerKey
    {
        Task<string> GetPlayerName();
        Task<int> GetPlayerScore();
        Task Win();
        Task Lose();
        Task SetPlayerName(string name);
        Task ForceDeactivate();
        Task ClearState();


    }
}