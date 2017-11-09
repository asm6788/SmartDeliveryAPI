using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static Smart_Delivery_API.Trackinginfo;

namespace Smart_Delivery_API
{
    public class Trackinginfo
    {
        public class TrackingDetail
        {
            public string trans_kind;
            public DeliveryLevel level;
            public string manName;
            public string manPic;
            public string trans_telno;
            public string trans_telno2;
            public DateTime trans_time;
            public string trans_where;
        }

        public enum DeliveryLevel
        {
            ERROR,
            Delivering,
            Collected,
            ArrivalAtBranch = 4,
            StartDelivery,
            CompleteDelivey
        }
        public string adUrl;
        public bool complete;
        public string estimate;
        public long invoice_no;
        public string itemImage;
        public string itemName;
        public string orderNumber;
        public string productInfo;
        public string reciver_addr;
        public string reciver_name;
        public string recipient;
        public string result;
        public string sender_name;
        public string zipCode;
        public DeliveryLevel level;
        public List<TrackingDetail> TrackingDetails = new List<TrackingDetail>();
    }
    public class SmartDelivery
    {
        string APIKEY = null;

        public SmartDelivery(string APIKEY)
        {
           this.APIKEY = APIKEY;
        }

        public enum RequestType
        {
            Companylist,
            Recommend,
            TrackingInfo
        }
        string RequsetHTTP(RequestType requestType,long invoice = 0,int Code = 0)
        {
            string strUri = "";
            if (requestType == RequestType.TrackingInfo)
            {
                strUri = "http://info.sweettracker.co.kr/api/v1/trackingInfo";
            }
            else
            {
                strUri = "http://info.sweettracker.co.kr/api/v1/" + requestType.ToString().ToLower();
            }

            // POST, GET 보낼 데이터 입력
            StringBuilder dataParams = new StringBuilder();
            dataParams.Append("?t_key="+APIKEY);
            if(requestType == RequestType.Recommend || requestType == RequestType.TrackingInfo)
            {
                if (requestType == RequestType.TrackingInfo)
                {
                    if (Code == 0)
                    {
                        throw new Exception("Code NOT SET");
                    }
                    dataParams.Append("&t_code=" + Code);
                }
                if (invoice == 0)
                {
                    throw new Exception("Invoice NOT SET");
                }
                dataParams.Append("&t_invoice=" + invoice);
            }
            HttpWebRequest wReq;
            HttpWebResponse wRes;
            Uri uri = new Uri(strUri + dataParams);
            wReq = (HttpWebRequest)WebRequest.Create(uri); // WebRequest 객체 형성 및 HttpWebRequest 로 형변환
            wReq.Method = "GET"; // 전송 방법 "GET" or "POST"
            wReq.ContentType = "application/xml;charset=UTF-8";
            /* POST 전송일 경우
            byte[] byteArray = Encoding.UTF8.GetBytes(data);
            zz
            Stream dataStream = wReq.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            */

            using (wRes = (HttpWebResponse)wReq.GetResponse())
            {
                Stream respPostStream = wRes.GetResponseStream();
                StreamReader readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("UTF-8"), true);
                string resResult = readerPost.ReadToEnd();
                return resResult;
            }
        }  
        /// <summary>
        /// 모든택배회사의 코드를 불러옵니다
        /// </summary>
        /// <returns></returns>
        public Dictionary<int,string> CompanyList()
        {
            if(APIKEY == null)
            {
                throw new Exception("APIKEY NOT SET");
            }
            XmlDocument xml = new XmlDocument(); // XmlDocument 생성
            xml.LoadXml(RequsetHTTP(RequestType.Companylist));
            XmlNodeList xnList = xml.GetElementsByTagName("Company"); //접근할 노드
            Dictionary<int, string> Result = new Dictionary<int, string>();
            foreach (XmlNode xn in xnList)
            {
                Result.Add(Convert.ToInt32(xn["Code"].InnerText), xn["Name"].InnerText);
            }
            return Result;
        }
        /// <summary>
        /// 해당송장번호의 택배회사를 추측해옵니다
        /// </summary>
        /// <param name="invoice">송장번호</param>
        /// <returns></returns>
        public Dictionary<int, string> RecommendList(long invoice)
        {
            if (APIKEY == null)
            {
                throw new Exception("APIKEY NOT SET");
            }
            XmlDocument xml = new XmlDocument(); // XmlDocument 생성
            xml.LoadXml(RequsetHTTP(RequestType.Recommend,invoice));
            XmlNodeList xnList = xml.GetElementsByTagName("Company"); //접근할 노드
            Dictionary<int, string> Result = new Dictionary<int, string>();
            foreach (XmlNode xn in xnList)
            {
                Result.Add(Convert.ToInt32(xn["Code"].InnerText), xn["Name"].InnerText);
            }
            return Result;
        }
        /// <summary>
        /// 택배의 상태를 추적합니다
        /// </summary>
        /// <param name="invoice">송장번호</param>
        /// <param name="Code">택배사번호</param>
        /// <returns></returns>
        public Trackinginfo Tracking(long invoice, int Code)
        {
            if (APIKEY == null)
            {
                throw new Exception("APIKEY NOT SET");
            }
            XmlDocument xml = new XmlDocument(); // XmlDocument 생성
            xml.LoadXml(RequsetHTTP(RequestType.TrackingInfo, invoice, Code));
            XmlNodeList xnList = xml.GetElementsByTagName("tracking_details"); //접근할 노드
            List<TrackingDetail> Details = new List<TrackingDetail>();
            foreach (XmlNode xn in xnList)
            {
                TrackingDetail trackingDetail = new TrackingDetail();
                Type type_info = typeof(TrackingDetail);

                System.Reflection.FieldInfo[] f =
                        type_info.GetFields();
                foreach (System.Reflection.FieldInfo _f in f)
                {
                    if (xn[_f.Name] != null)
                    {
                        if (_f.Name == "level")
                        {
                            if (xn["level"].InnerText == "3")
                            {
                                _f.SetValue(trackingDetail, DeliveryLevel.Delivering);
                            }
                            else
                            {
                                _f.SetValue(trackingDetail, (DeliveryLevel)Convert.ToInt32(xn["level"].InnerText));
                            }
                        }
                        else if (_f.Name == "trans_time")
                        {
                            _f.SetValue(trackingDetail, DateTime.Parse(xn["trans_time"].InnerText));
                        }
                        else
                        {
                            _f.SetValue(trackingDetail, xn[_f.Name].InnerText);
                        }
                    }

                }
                Details.Add(trackingDetail);
            }
            xnList = xml.GetElementsByTagName("tracking_info"); //접근할 노드
            Trackinginfo Result = new Trackinginfo();
            XmlNode xn_info = xnList[0];
            Trackinginfo trackinginfo = new Trackinginfo();
            Type type = typeof(Trackinginfo);

            System.Reflection.FieldInfo[] f_info =
                    type.GetFields();
            foreach (System.Reflection.FieldInfo _f in f_info)
            {
                if (xn_info[_f.Name] != null)
                {
                    if (_f.Name == "level")
                    {
                        if (xn_info["level"].InnerText == "3")
                        {
                            _f.SetValue(trackinginfo, DeliveryLevel.Delivering);
                        }
                        else
                        {
                            _f.SetValue(trackinginfo, (DeliveryLevel)Convert.ToInt32(xn_info["level"].InnerText));
                        }
                    }
                    else if (_f.Name == "trans_time")
                    {
                        _f.SetValue(trackinginfo, DateTime.Parse(xn_info["trans_time"].InnerText));
                    }
                    else
                    {
                        if (_f.Name == "complete")
                        {
                            if (xn_info["complete"].InnerText == "Y")
                            {
                                trackinginfo.complete = true;
                            }
                        }
                        else if (_f.Name == "invoice_no")
                        {
                            trackinginfo.invoice_no = Convert.ToInt64(xn_info["invoice_no"].InnerText);
                        }
                        else
                        {
                            _f.SetValue(trackinginfo, xn_info[_f.Name].InnerText);
                        }

                    }
                }
            }
            trackinginfo.TrackingDetails = Details;
            return trackinginfo;
        }
    }
}
