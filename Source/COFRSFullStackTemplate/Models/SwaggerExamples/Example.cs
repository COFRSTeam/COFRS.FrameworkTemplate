using $entitynamespace$;
using $resourcenamespace$;
using Swashbuckle.Examples;
using System;
using System.Collections.Generic;
$if$ ($usenpgtypes$ == true)using NpgsqlTypes;
$endif$$if$ ($examplebarray$ == true)using System.Collections;
$endif$$if$ ($exampleimage$ == true)using System.Drawing;
$endif$$if$ ($examplenet$ == true)using System.Net;
$endif$$if$ ($examplenetinfo$ == true)using System.Net.NetworkInformation;
$endif$using COFRS;

namespace $rootnamespace$
{
$exampleModel$

$exampleCollectionModel$}
