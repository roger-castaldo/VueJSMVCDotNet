﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Formats.Asn1.AsnWriter;

[assembly: Parallelize(Workers = 75, Scope = ExecutionScope.MethodLevel)]