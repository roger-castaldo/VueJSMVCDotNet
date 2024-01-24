using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 75, Scope = ExecutionScope.MethodLevel)]