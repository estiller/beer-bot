using System;

namespace BeerBot.BeerApi.Client.Models
{
    [Serializable]
    public partial class Style
    {
        public override string ToString()
        {
            return Name;
        }
    }
}