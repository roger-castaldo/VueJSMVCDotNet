using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 20, Scope = ExecutionScope.ClassLevel)]