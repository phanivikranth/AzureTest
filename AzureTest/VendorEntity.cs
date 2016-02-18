using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureTest
{
    class VendorEntity : TableEntity
    {
        public VendorEntity(string name, string code, string description,double price)
        {
            this.Name = name;
            this.Code = code;
            this.Description = description;
            this.Price = price;
        }

        public VendorEntity() { }

        public string Name { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public Double Price { get; set; }
    }

    class VendorProduct
    {
        public VendorProduct(string vendorCode, string productId)
        {
            this.VendorCode = vendorCode;
            this.ProductId = productId;            
        }
        public string VendorCode { get; set; }
        public string ProductId { get; set; }        
    }        
}
