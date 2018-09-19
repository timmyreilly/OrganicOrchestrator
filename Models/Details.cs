
using System;

namespace Company.Function{
    public class OrderLineItemModel{
        public string ponumber{get;set;}
        public string productid{get;set;}
        public int quantity{get;set;}
        public decimal totalcost{get;set;}


        public static OrderLineItemModel FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');
            OrderLineItemModel orderLineItemModel = new OrderLineItemModel();
            orderLineItemModel.ponumber = Convert.ToString(values[0]);
            orderLineItemModel.productid = Convert.ToString(values[1]);
            orderLineItemModel.quantity = Convert.ToInt32(values[2]);
            orderLineItemModel.totalcost = Convert.ToDecimal(values[3]);
            return orderLineItemModel;
        }
    }

    // ponumber,datetime,locationid,locationname,locationaddress,locationpostcode,totalcost,totaltax
    public class OrderHeaderDetailModel{
        public string ponumber {get; set;}
        public string datetime{get;set;}
        public string locationid{get;set;}
        public string locationname{get;set;}
        public string locationaddress{get;set;}
        public int locationpostcode{get;set;}
        public decimal totalcost{get;set;}
        public decimal totaltax{get;set;}

        // public static OrderHeaderDetailModel FromCsv(string csvLine)
        // {
        //     string[] values = csvLine.Split(','); 
        //     OrderHeaderDetailModel orderHeaderDetailModel = new OrderHeaderDetailModel();
        //     orderHeaderDetailModel.ponumber = Convert.ToString(values[0]); 
        //     orderHeaderDetailModel.datetime = Convert.ToString(values[1]); 
        //     orderHeaderDetailModel.locationid = Convert.ToString(values[2]); 
        //     orderHeaderDetailModel.locationname = Convert.ToString(values[3]); 
        //     orderHeaderDetailModel.locationaddress = Convert.ToString(values[4]); 
        //     orderHeaderDetailModel.locationpostcode = Convert.ToInt32(values[5]); 
        //     orderHeaderDetailModel.totalcost = Convert.ToDecimal(values[6]); 
        //     orderHeaderDetailModel.totaltax = Convert.ToDecimal(values[7]); 
        //     return orderHeaderDetailModel; 
        // }
    }

    public class ProductInformationModel{
        public string productid{get;set;}
        public string productname{get;set;}
        public string productdescription{get;set;}

        public static ProductInformationModel FromCsv(string csvLine) 
        {
            string[] values = csvLine.Split(',');
            ProductInformationModel productInformationModel = new ProductInformationModel(); 
            productInformationModel.productid = Convert.ToString(values[0]); 
            productInformationModel.productname = Convert.ToString(values[1]); 
            productInformationModel.productdescription = Convert.ToString(values[2]);
            return productInformationModel;  
        }
    }

    public class CosmosEntry {
        string ponumber{get;set;}
        string locationid{get;set;}
        string locationname{get;set;}
        string locationaddress{get;set;}
        int locationpostcode{get;set;}
        decimal totalcost{get;set;}
        decimal totaltax{get;set;}

        OrderLineItemModel[] orderLineItems; 
    }



    
}