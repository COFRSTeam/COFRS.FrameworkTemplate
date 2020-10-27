using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using $entitynamespace$;
using $domainnamespace$;
using COFRS;
$if$ ($singleexamplenamespace$ != none)using $singleexamplenamespace$;
$endif$using Swashbuckle.Examples;
using Swashbuckle.Swagger.Annotations;
using $orchestrationnamespace$;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Http;
$if$ ($validationnamespace$ != "")using $validationnamespace$;
$endif$$if$ ($securitymodel$ == OAuth)using Microsoft.Owin.Security.Authorization.WebApi;
$endif$using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using RouteAttribute = System.Web.Http.RouteAttribute;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;
using HttpPutAttribute = System.Web.Http.HttpPutAttribute;
using HttpPatchAttribute = System.Web.Http.HttpPatchAttribute;
using HttpDeleteAttribute = System.Web.Http.HttpDeleteAttribute;

namespace $rootnamespace$
{
$model$}
