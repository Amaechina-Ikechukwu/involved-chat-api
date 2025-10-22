using System.Collections.Generic;

namespace Involved_Chat.DTOS
{
    public class PaginatedUserResponse
    {
        public IEnumerable<NearbyUserDto> Users { get; set; } = new List<NearbyUserDto>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public bool HasNextPage { get; set; }
    }
}