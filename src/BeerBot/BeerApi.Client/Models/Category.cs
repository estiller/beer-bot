using System;

namespace BeerBot.BeerApi.Client.Models
{
    [Serializable]
    public partial class Category
    {
        public override string ToString()
        {
            return Name;
        }
    }
}