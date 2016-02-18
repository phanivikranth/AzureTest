using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureTest
{
    public partial class Form1 : Form
    {
        internal const string TableName = "VendorData";
        internal const string QueueName = "productupdates";
        public Form1()
        {
            InitializeComponent();
            CreateMyListView();
        }

        private static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the application.");
                throw;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            return storageAccount;
        }
       
        private static void AdvancedTableOperationsAsync(CloudTable table,ListView list)
        {           
            PartitionRangeQueryAsync(table, list);            
        }
        private static void PartitionRangeQueryAsync(CloudTable table, ListView list)
        {
            TableQuery<VendorEntity> rangeQuery1 = new TableQuery<VendorEntity>().Take(50);
            int i = 0;
             
            TableContinuationToken token = null;
            TableQuerySegment<VendorEntity> segment;
            do
            {
                try {
                    segment = table.ExecuteQuerySegmented(rangeQuery1, token);
                    token = segment.ContinuationToken;
                    foreach (VendorEntity entity in segment)
                    {
                        i++;
                        ListViewItem item1 = new ListViewItem(entity.PartitionKey,0);
                        //item1.SubItems.Add(entity.PartitionKey);
                        item1.SubItems.Add(entity.RowKey);
                        item1.SubItems.Add(entity.Description);                        
                        list.Items.AddRange(new ListViewItem[] { item1 });
                        if (i == 50)
                        {
                            token = null;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    //e.InnerException.ToString();
                }
            }
            while (token != null);
        }
        private static CloudTable CreateTableAsync()
        {
            
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString("DefaultEndpointsProtocol=http;AccountName=boutydata;AccountKey=tugVuwvqtJbgaBKChCp1asGvE2ruBiiqAaTDwlTt9P8FxXvz0+y0mmHwX/wnacIovSnyxBpyJrG8ufHE8Cln9g==");            
            CloudTableClient tableClient = new CloudTableClient(storageAccount.TableStorageUri, storageAccount.Credentials);
            CloudTable table = tableClient.GetTableReference(TableName);
            return table;
        }

        private static CloudQueue CreateQueueAsync()
        {
            
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString("DefaultEndpointsProtocol=http;AccountName=boutydata;AccountKey=tugVuwvqtJbgaBKChCp1asGvE2ruBiiqAaTDwlTt9P8FxXvz0+y0mmHwX/wnacIovSnyxBpyJrG8ufHE8Cln9g==");            
            CloudQueueClient queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageAccount.Credentials);            
            CloudQueue Queue = queueClient.GetQueueReference(QueueName);                        
            return Queue;
        }

        private static void BasicQueueOperationsAsync(CloudQueue queue,ListView list)
        {                        
            //CloudQueueMessage peekedMessage = queue.PeekMessage();

            //queue.FetchAttributes();
            //int? cachedMessageCount = queue.ApproximateMessageCount;

            CloudQueueMessage message = queue.GetMessage();
            if (message != null)
            {                
                queue.DeleteMessage(message);
            }

            if (message != null)
            {
                
                //t1.Text = message.AsString;
                //t1.Text += message.PopReceipt + message.InsertionTime + message.ExpirationTime;                

                string json = message.AsString;

                JObject googleSearch = JObject.Parse(json);
                IList<JToken> results = googleSearch["Updates"].Children().ToList();
                IList<VendorProduct> searchResults = new List<VendorProduct>();

                foreach (JToken result in results)
                {
                    VendorProduct searchResult = JsonConvert.DeserializeObject<VendorProduct>(result.ToString());
                    searchResults.Add(searchResult);
                }

                int i = 0;
                foreach (VendorProduct entity in searchResults)
                {

                    CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString("DefaultEndpointsProtocol=http;AccountName=boutydata;AccountKey=tugVuwvqtJbgaBKChCp1asGvE2ruBiiqAaTDwlTt9P8FxXvz0+y0mmHwX/wnacIovSnyxBpyJrG8ufHE8Cln9g==");
                    CloudTableClient tableClient = new CloudTableClient(storageAccount.TableStorageUri, storageAccount.Credentials);
                    CloudTable table = tableClient.GetTableReference(TableName);

                    TableQuery<VendorEntity> rangeQuery1 = new TableQuery<VendorEntity>().Where(TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,entity.VendorCode),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey",QueryComparisons.Equal,"Product__"+entity.ProductId)));

                    

                    TableContinuationToken token = null;
                    TableQuerySegment<VendorEntity> segment;
                    do
                    {
                        try
                        {
                            segment = table.ExecuteQuerySegmented(rangeQuery1, token);
                            token = segment.ContinuationToken;
                            foreach (VendorEntity entity1 in segment)
                            {
                                i++;
                                ListViewItem item1 = new ListViewItem(entity1.PartitionKey,0);
                                //item1.SubItems.Add(entity1.PartitionKey);
                                item1.SubItems.Add(entity1.Name);
                                item1.SubItems.Add(entity1.Description);
                                item1.SubItems.Add(entity1.Price.ToString());
                                list.Items.AddRange(new ListViewItem[] { item1 });
                                if (i == searchResults.Count)
                                {
                                    token = null;
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            //e.InnerException.ToString();
                        }
                    }
                    while (token != null);                    
                }
            }

                     

            //CloudQueueMessage message = await queue.GetMessageAsync();
            //if (message != null)
            //{
            //    Console.WriteLine("Processing & deleting message with content: {0}", message.AsString);
            //    await queue.DeleteMessageAsync(message);
            //}
        }

        private void CreateMyListView()
        {
            
            ListView listView1 = new ListView();
            listView1.Bounds = new Rectangle(new Point(10, 10), new Size(600, 600));                        
            listView1.View = View.Details;            
            
            listView1.AllowColumnReorder = true;            
            
            listView1.FullRowSelect = true;
            
            listView1.GridLines = true;
            
            listView1.Sorting = SortOrder.Ascending;

            //listView1.Columns.Add("Item", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Code", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Description", -2, HorizontalAlignment.Left);

            ListView listView2 = new ListView();
            listView2.Bounds = new Rectangle(new Point(610, 10), new Size(600, 600));
            listView2.View = View.Details;

            listView2.AllowColumnReorder = true;

            listView2.FullRowSelect = true;

            listView2.GridLines = true;

            listView2.Sorting = SortOrder.Ascending;

            //listView2.Columns.Add("Item", -2, HorizontalAlignment.Left);
            listView2.Columns.Add("VendorName", -2, HorizontalAlignment.Left);
            listView2.Columns.Add("ProductName", -2, HorizontalAlignment.Left);
            listView2.Columns.Add("ProductDescription", -2, HorizontalAlignment.Left);
            listView2.Columns.Add("Price", -2, HorizontalAlignment.Left);

            CloudTable table = CreateTableAsync();
            AdvancedTableOperationsAsync(table,listView1);
            this.Controls.Add(listView1);

            //TextBox t1 = new TextBox();
            //t1.Bounds = new Rectangle(new Point(10, 610), new Size(500, 500));
            CloudQueue queue = CreateQueueAsync();
            BasicQueueOperationsAsync(queue,listView2);

            //this.Controls.Add(t1);
            this.Controls.Add(listView2);

            //Button b1 = new Button();
            //b1.Bounds = new Rectangle(new Point(1210, 10), new Size(20, 20));
            //b1.Text = "Refresh Screen";
            //b1.Click += B1_Click;            
            ////this.Controls.Remove(listView2);
            //this.Controls.Add(b1);           
        }

        //private void B1_Click(object sender, EventArgs e)
        //{
        //    CreateMyListView();
        //}
    }
}
