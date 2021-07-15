using Microsoft.XmlDiffPatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace SimpleComparingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string folderPath;

            if (args.Length != 0)
            {
                folderPath = args[0];
            }
            else
            {
                Console.Write("Insert path to the folder: ");
                folderPath = Console.ReadLine();
            }

            //Get origin list of files in folder
            string[] fileArray = Directory.GetFiles(folderPath);
            for (int i=0;  i<fileArray.Length; i++)
            {                
                //Get info about current file
                FileInfo baseFileInfo = new FileInfo(fileArray[i]);

                //Process file ONLY if it hasn't been processed yet
                if (!baseFileInfo.Name.Contains("old"))
                {
                    //Assume that current file is the oldest one
                    FileInfo oldestVersionFile = new FileInfo(fileArray[i]);
                    oldestVersionFile = baseFileInfo;

                    //List of ALL identical files
                    List<FileInfo> identicalFiles = new List<FileInfo>();
                    identicalFiles.Add(baseFileInfo);

                    //Compare current file with all the rest that haven't been processed yet
                    for (int j = i + 1; j < fileArray.Length; j++)
                    {
                        FileInfo sampleFileInfo = new FileInfo(fileArray[j]);
                        if ((!sampleFileInfo.Name.Contains("old")) && (FilesAreEqual(baseFileInfo, sampleFileInfo)))
                        {
                            //Check creation time and update info about the oldest file if needed
                            if (sampleFileInfo.CreationTime < oldestVersionFile.CreationTime)
                                oldestVersionFile = sampleFileInfo;

                            //Add file to list of identical
                            identicalFiles.Add(sampleFileInfo);
                        }
                    }

                    //if there any identical file exists then rename as old, old1, old2, etc.
                    if (identicalFiles.Count > 1)
                    {
                        int oldIndex = 1;
                        foreach (FileInfo fi in identicalFiles)
                        {
                            if (fi.Name != oldestVersionFile.Name)
                            {
                                File.Move(fi.FullName, $@"{fi.Directory}/{fi.Name.Split('.').First()}-old{oldIndex}{fi.Extension}");
                                oldIndex++;
                            }

                        }
                        File.Move(oldestVersionFile.FullName, $@"{oldestVersionFile.Directory}/{oldestVersionFile.Name.Split('.').First()}-old{oldestVersionFile.Extension}");

                        //Update list of files after renaming as it has been changed
                        fileArray = Directory.GetFiles(folderPath);
                    }
                }
            }
            Console.ReadLine();
        }

        static bool FilesAreEqual(FileInfo baseFile, FileInfo sampleFile)
        {
             //Comparing by size
            if (baseFile.Length != sampleFile.Length)
            {
                Console.WriteLine($"{baseFile.Name} and {sampleFile.Name} are not identical on size");
                return false;
            }

            //Comparing by content
            XmlDiff xmldiff = new XmlDiff(XmlDiffOptions.IgnoreChildOrder |
                                          XmlDiffOptions.IgnoreNamespaces |
                                          XmlDiffOptions.IgnorePrefixes);

            string diffgramPath = baseFile.Directory + @"\diffgram.xml";
            XmlWriter diffgramWriter = new XmlTextWriter(diffgramPath, new System.Text.UnicodeEncoding());
            bool compareResult = xmldiff.Compare(baseFile.FullName, sampleFile.FullName, false, diffgramWriter);
            diffgramWriter.Flush();
            diffgramWriter.Close();

            if (!compareResult)
            {
                //Check diifgram.xml and find first change node
                XmlDocument diffgramXml = new XmlDocument();
                diffgramXml.Load(diffgramPath);
                var nsmgr = new XmlNamespaceManager(diffgramXml.NameTable);
                nsmgr.AddNamespace("xd", "http://schemas.microsoft.com/xmltools/2002/xmldiff");
                var node = diffgramXml.SelectSingleNode("//xd:change", nsmgr);
                Console.WriteLine($"{baseFile} and {sampleFile} are not identical on string: {node.InnerText}");
                
            }

            File.Delete(diffgramPath);
            return compareResult;
        }

    }
}
