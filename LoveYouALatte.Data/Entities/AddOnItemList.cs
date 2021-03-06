using System;
using System.Collections.Generic;

#nullable disable

namespace LoveYouALatte.Data.Entities
{
    public partial class AddOnItemList
    {
        public int AddOnListId { get; set; }
        public int CartAddOnItemId { get; set; }
        public int AddOnId { get; set; }
        public int Quantity { get; set; }
        public decimal AddOnUnitPrice { get; set; }
        public decimal AddOnTotalPrice { get; set; }

        public virtual AddOn AddOn { get; set; }
        public virtual CartAddOnItem CartAddOnItem { get; set; }
    }
}
