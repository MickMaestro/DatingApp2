namespace DatingApp.API.Helpers
{
    public class UserParams
    {
        private const int MaxPageSize = 50;// stops the user from requesting too many people
        public int PageNumber { get; set; } = 1;// always returns the first page unless specified otherwise
        private int pageSize = 10;
        public int PageSize
        {
            get { return pageSize;}
            set { pageSize = (value > MaxPageSize) ? MaxPageSize : value;}// if the value is greater than the maxPageSize set
            // the pageSize to the MaxPageSize otherwise set it to the value requested
        }// propfull; manually set the GET & SET properties

        public int UserId { get; set; }
        public string Gender { get; set; }
        public int MinAge { get; set; } = 18;
        public int MaxAge { get; set; } = 99;
        public string OrderBy { get; set; }
        public bool Likees { get; set; } = false;
        public bool Likers { get; set; } = false;
    }
}