using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;

public class Libraries_exists
{
    // A Test behaves as an ordinary method
    [Test]
    public void Libraries_existsSimplePasses()
    {
        throw new Exception("We migrated dependencies to a package. We need to rework this check before reusing it.");

        // string filePath = "Assets/Gothic-UnZENity-Core/Tests/EditorTests/FilesCheck/dll-List.txt";
        // Assert.IsTrue(File.Exists(filePath), "File " + filePath + " not found.");
        //
        // string[] dllFiles = File.ReadAllLines(filePath);
        // foreach (string dllFile in dllFiles) {
        //     if (!File.Exists(Path.Combine("Assets/Gothic-UnZENity-Core/Dependencies/", dllFile))) {
        //         Assert.Fail("File " + dllFile + " not found.");
        //     }
        // }
    }
}
