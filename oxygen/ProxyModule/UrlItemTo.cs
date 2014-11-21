namespace Proxygen
{
    public class UrlItemTo : UrlItem
    {


    
        /// <summary>
        /// this will be returned and no request will go to destination. if 'RespondWithCode' is specified, it will be used, else it will return with a 200
        /// </summary>
        public string Return { set; get; }

        /// <summary>
        /// specify response code to be used , override any response code in actual response 
        /// </summary>
        public int? RespondWithCode { set; get; }



        /// <summary>
        /// for diagnostics
        /// </summary>
        public bool IsSuccessful { set; get; }
     
    }
}