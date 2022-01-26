using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace AddMoviePosterToFolder {
    class Program {
        static void Main(string[] args) {
            var ParentFolder = GetParentFolder(args);
            var FoldersToSearch = GetListOfFoldersWithoutPosters( ParentFolder );

            foreach (var Folder in FoldersToSearch) {
                try {
                    var MovieTitle = GetNormalizedTitle( Folder );
                    var URLToFile = SearchPosterInBing( "\"" + MovieTitle + "\" poster" );
                    if (URLToFile != null) {
                        DownloadAndSaveImageToFolder(URLToFile, Folder);
                        Console.WriteLine($"OK. Added poster for {MovieTitle}");
                    }
                    else {
                        Console.WriteLine($"Did not find poster for {MovieTitle}");
                }
                }
                catch (Exception ex) {
                    Console.WriteLine($"ERROR. {Folder}. {ex.Message}");
                }
            }
        }

        private static void DownloadAndSaveImageToFolder(string url, string folder) {
            using (var client = new WebClient()) {
                client.DownloadFile(url, Path.Combine(folder, "folder.jpg"));
            }
        }

        private static object GetNormalizedTitle(string fullFolder) {
            // expect the title like Flora.and.Ulysses.2021.720p.DSNP.WEBRip.DDP5.1.Atmos.x264-TOMMY
            // Then parse until find the year
            // Will be converted to "Flora and Ulysses (2021)"
            var retval = string.Empty;
            var folder = new DirectoryInfo(fullFolder).Name;
            var words = folder.Split(' ', '.', '(', ')', '[', ']', '_', '-');
            foreach (var word in words) {
                if (!string.IsNullOrEmpty(retval) && IsAYear(word)) {
                    retval += $"({word})";
                    return retval;
                }
                retval += word + " ";
            }
            return retval;
        }

        private static List<string> GetListOfFoldersWithoutPosters(string ParentFolder) {
            var retval = new List<string>();
            var AllFolders = Directory.GetDirectories(ParentFolder, "", SearchOption.TopDirectoryOnly);
            foreach (var Folder in AllFolders) {
                if (!File.Exists(Path.Combine(Folder, "folder.jpg"))) {
                    retval.Add( Folder );
                }
            }
            return retval;
        }

        private static string GetParentFolder(string[] args) {
            string retval = Directory.GetCurrentDirectory();
            if (args != null && !string.IsNullOrEmpty(args[0])) {
                retval = args[0];
                if (!Directory.Exists(retval)) {
                    throw new ApplicationException($"The folder {retval} does not exists");
                }
            }
            return retval;
        }

        private static bool IsAYear(string word) {
            if (Int32.TryParse(word, out int year)) {
                return year > 1900 && year <= DateTime.Today.Year + 1; // maybe it's from next year ¯\_(ツ)_/¯
            }
            return false;
        }

        private static string SearchPosterInBing(string MovieTitle) {
            // Google images it's all obfuscated to avoid this, so we go Bing!
            // Some foreing/not popular movies fail to get a good poster
            // Home someone can improve it

            var url = @$"https://www.bing.com/images/search?q={MovieTitle}";
            using (var client = new WebClient()) {
                var response = client.DownloadString(url);

                //first remove all navigation
                response = response.Substring(response.IndexOf("rfPane")); 

                while (true) {
                    // parse by hand to avoid importing other libraries
                    var toParseIndexJpg = response.IndexOf(".jpg") + ".jpg".Length; 
                    var toParseIndexJpeg = response.IndexOf(".jpeg") + ".jpeg".Length; // sometime the best results are jpeg
                    var toParseIndex = Math.Min(toParseIndexJpg, toParseIndexJpeg);
                    if (toParseIndex < 0) break;
                    var toParse = response.Substring(0, toParseIndex);
                    var startIndex = toParse.LastIndexOf("http");
                    if (startIndex > 0) { 
                        var possibleResult = toParse.Substring(startIndex);
                        return possibleResult;
                    }
                    
                    //keep only the remaining to parse
                    response = response.Substring(toParseIndex + 1); 
                }
            }
            return null;
        }

    }
}
