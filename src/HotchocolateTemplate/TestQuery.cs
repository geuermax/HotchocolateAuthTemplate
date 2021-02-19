using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotchocolateTemplate
{
    [ExtendObjectType(Name = "Query")]
    [Authorize]
    public class TestQuery
    {
        public string GetTest() => "test";
    }
}
