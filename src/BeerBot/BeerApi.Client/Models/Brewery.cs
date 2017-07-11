using System;

namespace BeerBot.BeerApi.Client.Models
{
    [Serializable]
    public partial class Brewery
    {
        public override string ToString()
        {
            return Name;
        }
    }
}