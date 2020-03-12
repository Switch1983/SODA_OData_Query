using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace SODAODataClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    //This code is a sample only, there are several other method to implement an OData client but this is intended
    //to demonstrate token aquisition from Azure Access control Service
    public partial class MainWindow : Window
    {
        //values set here are consistent with values set by RED on Azure access control service
        static string serviceNamespace = "urbanwater";
        static string acsHostUrl = "accesscontrol.windows.net";
        //realm will be changing to https but for now we are using http
        static string realm = "http://urbanwater.cloudapp.net/WaterMeterData.svc";
        static string uid = "urbanw";
        static string pwd = "hT4mC#dO";

        public string strToken;

        public MainWindow()
        {
            InitializeComponent();

        }

        private void btnDisplayAll_Click(object sender, RoutedEventArgs e)
        {
            //get a token from Azure Access Control Service
            string token = GetTokenFromACS(realm);

            WebClient client = new WebClient();
            //include token on header
            string headerValue = string.Format("WRAP access_token=\"{0}\"", token);
            client.Headers.Add("Authorization", headerValue);

            //stream data from cloud platform
            Stream stream = client.OpenRead(@"http://urbanwater.cloudapp.net/WaterMeterData.svc/MeterReadings");
            
            StreamReader reader = new StreamReader(stream);
            String response = reader.ReadToEnd();

            outputtxtbox.Text = response;
        }


        // ACS (Azure Access Control Service) by passing the realm, username and password 
        // will return a Simple Web Token (SWT), this is inclused in the web service header for authentication.
        // These tokens have a life time set to a year for this example but for the final version the token life-time 
        // will be about 24 hours. 

        private static string GetTokenFromACS(string scope)
        {
            string wrapPassword = pwd;
            string wrapUsername = uid;
            
            // request a token from ACS
            WebClient client = new WebClient();
            client.BaseAddress = string.Format("https://{0}.{1}", serviceNamespace, acsHostUrl);

            NameValueCollection values = new NameValueCollection();
            values.Add("wrap_name", wrapUsername);
            values.Add("wrap_password", wrapPassword);
            values.Add("wrap_scope", scope);

            byte[] responseBytes = client.UploadValues("WRAPv0.9/", "POST", values);
            string response = Encoding.UTF8.GetString(responseBytes);

            return HttpUtility.UrlDecode(
                response
                .Split('&')
                .Single(value => value.StartsWith("wrap_access_token=", StringComparison.OrdinalIgnoreCase))
                .Split('=')[1]);
        }

        private void btnSpecificMeter_Click(object sender, RoutedEventArgs e)
        {
            //get a token from Azure Access Control Service
            string token = GetTokenFromACS(realm);

            WebClient client = new WebClient();
            //include token on header
            string headerValue = string.Format("WRAP access_token=\"{0}\"", token);
            client.Headers.Add("Authorization", headerValue);

            //stream data from cloud platform by changing url we can query the data-set.
            // A full list is provided by RED
            Stream stream = client.OpenRead(@"http://urbanwater.cloudapp.net/WaterMeterData.svc/MeterReadings?$filter=PartitionKey eq '417061779'");

            StreamReader reader = new StreamReader(stream);
            String response = reader.ReadToEnd();
            txtBoxSec.Text = "http://urbanwater.cloudapp.net/WaterMeterData.svc/MeterReadings?$filter=PartitionKey eq '417061779'";
            outputtxtbox.Text = response;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //get a token from Azure Access Control Service
            string token = GetTokenFromACS(realm);

            WebClient client = new WebClient();
            //include token on header
            string headerValue = string.Format("WRAP access_token=\"{0}\"", token);
            client.Headers.Add("Authorization", headerValue);

            //stream data from cloud platform by changing url we can query the data-set.
            // A full list is provided by RED
            Stream stream = client.OpenRead(@"http://urbanwater.cloudapp.net/WaterMeterData.svc/MeterReadings");

    
            StreamReader reader = new StreamReader(stream);

            String responses = reader.ReadToEnd();
            string needle = "MeterID";



             int count = 0;
             count = Regex.Matches(responses, "MeterID").Count;
            //     count = ((responses.Length - responses.Replace(needle, "").Length) / needle.Length) / 2;
             count = count / 2;
            tbCount.Text = count.ToString();
           
            //outputtxtbox.Text = response;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //get a token from Azure Access Control Service
             strToken = GetTokenFromACS(realm);
             txtBoxSec.Text = strToken.ToString();

        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {

            WebClient client = new WebClient();
            //include token on header
            string headerValue = string.Format("WRAP access_token=\"{0}\"", strToken);
            client.Headers.Add("Authorization", headerValue);

            //stream data from cloud platform by changing url we can query the data-set.
            // A full list is provided by RED
            Stream stream = client.OpenRead(txtBoxExec.Text);


            StreamReader reader = new StreamReader(stream);
            String response = reader.ReadToEnd();

            XNamespace xmlns = "http://www.w3.org/2005/Atom";
            XNamespace xmlnsd = "http://schemas.microsoft.com/ado/2007/08/dataservices";
            XNamespace xmlnsm = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

            var xDoc = XDocument.Parse(response);


            XName nodeName = xmlnsd + "meterid";
            //var phone = xEle.Elements("Employee").Elements("Phone").ToList();

            var test = xDoc.Descendants(xmlns + "feed").Elements(xmlns + "entry").Elements(xmlns + "content").Elements(xmlnsm + "properties").ToList();

            var entries = (from docs in xDoc.Descendants(xmlns + "feed").Elements(xmlns + "entry").Elements(xmlns + "content").Elements(xmlnsm + "properties")
                         select new
                         {
                             meterid = docs.Element(xmlnsd + "MeterID").Value,
                             metertime = docs.Element(xmlnsd + "RowKey").Value,
                             meterreading = docs.Element(xmlnsd + "EncryptedReadings").Value
                         }).ToList();

            DataTable dt = new DataTable();
            dt.Columns.Add("Meter ID");
            dt.Columns.Add("Meter Time");
            dt.Columns.Add("Meter Encrypted");
            dt.Columns.Add("Meter Reading");

            List<string> results = new List<string>();
            int i = 0;
            //Get Key from database
            foreach (var entry in entries)
            {
                string strMeterKey = string.Empty;
                using (var db = new uwkeydataEntities())
                {
                    foreach (MeterKey mk in db.MeterKeys)
                    {
                        if (mk.MeterID == entry.meterid)
                        {
                            strMeterKey = mk.AesKey;
                            break;
                        }
                    }

                    string strDecrypt = DecryptMeterReadings(strMeterKey, entry.meterreading);

                    if (strDecrypt != string.Empty)
                    {
                        DataRow newrow = dt.NewRow();
                        newrow["Meter ID"] = entry.meterid;
                        newrow["Meter Time"] = entry.metertime;
                        newrow["Meter Encrypted"] = entry.meterreading;
                        newrow["Meter Reading"] = strDecrypt;
                        dt.Rows.Add(newrow);
                             
                    }
                }
                //entry.meterid.ToString()
            }

            //Decrypt Using database Key

            //dg.ItemsSource = entries;
            dg.ItemsSource = dt.AsDataView();
            outputtxtbox.Text = response;

            int count = 0;
            count = Regex.Matches(response, "entry").Count;
            //     count = ((responses.Length - responses.Replace(needle, "").Length) / needle.Length) / 2;
            count = count / 2;
            tbCount.Text = count.ToString();


            
        }

        private const string AesIV = @"!QAZ2WSX#EDC4RFV";
        private const string AesKey = @"5TGB&YHN7UJM(IK<";
        private string DecryptMeterReadings(string strMeterKey, string text)
        {
             AesCryptoServiceProvider aes;
                    // AesCryptoServiceProvider
             using (aes = new AesCryptoServiceProvider())
             {
                 aes.BlockSize = 128;
                 aes.KeySize = 128;
                 aes.IV = Encoding.UTF8.GetBytes(AesIV);
                 //aes.Key = Encoding.UTF8.GetBytes(AesKey);
                 aes.Key = System.Convert.FromBase64String(strMeterKey);
                 aes.Mode = CipherMode.CBC;
                 aes.Padding = PaddingMode.PKCS7;

                 using (ICryptoTransform decryptor = aes.CreateDecryptor())
                 {
                     //Get bytes from encrypted string
                     byte[] ecnryptedString = System.Convert.FromBase64String(text);
                     byte[] original = decryptor.TransformFinalBlock(ecnryptedString, 0, ecnryptedString.Length);

                     string final = Encoding.Unicode.GetString(original);
                     return final;
                 }
             }
        }

      
    }

}
