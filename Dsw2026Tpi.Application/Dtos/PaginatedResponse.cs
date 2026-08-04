namespace Dsw2026Tpi.Application.Dtos
{
    public class PaginatedResponse<T>
    {
        public int pageSize { get; set; }
        public int pageIndex { get; set; }
        public int Total { get; set; }
        public IEnumerable<T> data{ get; set; }=new List<T>();
    }
}
