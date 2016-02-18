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
using System.Configuration;

namespace AzureTest
{
    public partial class Form1 : Form
    {
        internal const string TableName = "VendorData";
        internal const string QueueName = "productupdates";
        ListView listView2 = new ListView();
        
        public Form1()
        {
            InitializeComponent();
            CreateMyListView();

            System.Windows.Forms.Timer timer1 = new System.Windows.Forms.Timer();
            timer1.Interval = 5000;
            timer1.Tick += new System.EventHandler(timer1_Tick);
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //listView1.Items.Clear();
            listView2.Items.Clear();
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
       
        private static void AdvancedTableOperations(CloudTable table,ListView list)
        {           
            PartitionRangeQuery(table, list);            
        }
        private static void PartitionRangeQuery(CloudTable table, ListView list)
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
        private static CloudTable CreateTable()
        {
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["storageConnectionString"].ConnectionString);            
            CloudTableClient tableClient = new CloudTableClient(storageAccount.TableStorageUri, storageAccount.Credentials);
            CloudTable table = tableClient.GetTableReference(TableName);
            return table;
        }

        private static CloudQueue CreateQueue()
        {
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["storageConnectionString"].ConnectionString);
            CloudQueueClient queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageAccount.Credentials);            
            CloudQueue Queue = queueClient.GetQueueReference(QueueName);                        
            return Queue;
        }

        private static void BasicQueueOperations(CloudQueue queue,ListView list)
        {                                    
            CloudQueueMessage message = queue.GetMessage();
            if (message != null)
            {                
                queue.DeleteMessage(message);
            }

            if (message != null)
            {                               
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

                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["storageConnectionString"].ConnectionString);
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
                        }
                    }
                    while (token != null);                    
                }
            }                               
        }

        private void CreateMyListView()
        {
            
            ListView listView1 = new ListView();

            //listView1.Items.Clear();           
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

            //ListView listView2 = new ListView();
            listView1.Items.Clear();
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

            CloudTable table = CreateTable();
            AdvancedTableOperations(table,listView1);
            this.Controls.Add(listView1);

            //TextBox t1 = new TextBox();
            //t1.Bounds = new Rectangle(new Point(10, 610), new Size(500, 500));
            CloudQueue queue = CreateQueue();
            BasicQueueOperations(queue,listView2);

            //this.Controls.Add(t1);            
            this.Controls.Add(listView2);                                               
        }        
    }
}
