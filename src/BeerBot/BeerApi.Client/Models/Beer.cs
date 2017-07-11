using System;

namespace BeerBot.BeerApi.Client.Models
{
    [Serializable]
    public partial class Beer
    {
        public override string ToString()
        {
            return Name;
        }
    }
}