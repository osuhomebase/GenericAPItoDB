using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
//using System.Web.Script.Serialization;

namespace Api2db
{
    public class GenericAPIHelper
    {
        private string _UserName { get; set; }
        private string _Password { get; set; }

        public GenericAPIHelper(string UserName, string Password)
        {
            _UserName = UserName;
            _Password = Password;
        }
        public object[] GetWebServiceResult(string wUrl)
        {

            HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(wUrl);
            httpWReq.Credentials = new NetworkCredential(_UserName, _Password);
            httpWReq.Timeout = 300000; // prevent response from timing out

            HttpWebResponse httpWResp = (HttpWebResponse)httpWReq.GetResponse();
            object[] jsonResponse = null;

            httpWResp.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(_UserName + ":" + _Password));

            try
            {
                //Test the connection
                if (httpWResp.StatusCode == HttpStatusCode.OK)
                {
                    Stream responseStream = httpWResp.GetResponseStream();
                    string jsonString = null;

                    //Set jsonString using a stream reader
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        jsonString = reader.ReadToEnd().Replace("\\", "");
                        reader.Close();
                    }

                    //Deserialize our JSON
                    //JavaScriptSerializer sr = new JavaScriptSerializer();
                    //sr.MaxJsonLength = Int32.MaxValue;
                    //JSON string comes in with a leading and trailing " that need to be removed for parsing to work correctly
                    //The JSON here is serialized weird, normally you would not need this trim
                    jsonResponse = JsonConvert.DeserializeObject<object[]>(jsonString);
                    //jsonResponse = sr.Deserialize<object[]>(jsonString);
                }
                //Output connection error message
                else
                {
                    Console.WriteLine(httpWResp.StatusCode.ToString());
                }
            }
            //Output JSON parsing error
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            //Console.ReadLine();

            return jsonResponse;

        }

    }
}
