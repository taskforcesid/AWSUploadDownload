using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


// To interact with Amazon S3.
using Amazon.S3;
using Amazon.S3.Model;
using System.Collections.Generic;
using Amazon.Runtime.CredentialManagement;
using Amazon;

namespace AWSUploadDownload
{
    class Program
    {
        static void AuthenticateAWSUser()
        {
            var options = new CredentialProfileOptions
            {
                AccessKey = "AKIAXILVDSA2ROBR3CIJ",
                SecretKey = "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"
            };
            var profile = new Amazon.Runtime.CredentialManagement.CredentialProfile("mylmuser", options);
            profile.Region = RegionEndpoint.USWest1;
            var netSDKFile = new Amazon.Runtime.CredentialManagement.NetSDKCredentialsFile();
            netSDKFile.RegisterProfile(profile);

        }

        // Function Main
        static async Task Main(string[] args)
        {
            AuthenticateAWSUser();
            // Create an S3 client object.
            var client = new AmazonS3Client();
            S3Bucket activeBucket = new S3Bucket();
            // List the buckets owned by the user.
            try
            {
                Console.WriteLine();
                Console.WriteLine("Getting a list of your buckets...");
                Console.WriteLine();

                var response = await client.ListBucketsAsync();

                //This application should return only ONE bucket
                Console.WriteLine($"Number of buckets: {response.Buckets.Count}");
                activeBucket = response.Buckets[0];

                Console.WriteLine("ACTIVE BUCKET: {0}", activeBucket.BucketName);
                Console.WriteLine("Files in bucket:");


                ListObjectsV2Response ListObjectsResponse = new ListObjectsV2Response();
                // Retrieve the files in the bucket
                try
                {
                    ListObjectsV2Request request = new ListObjectsV2Request
                    {
                        BucketName = activeBucket.BucketName,
                        MaxKeys = 10
                    };
                    do
                    {
                        ListObjectsResponse = await client.ListObjectsV2Async(request);
                        string keyName = "";
                        long bufferSize = 0;
                        // Process the response.
                        int keyIndex = 0;
                        foreach (S3Object entry in ListObjectsResponse.S3Objects)
                        {
                            Console.WriteLine("File ID = {0} key = {1} size = {2}",
                                ++keyIndex, entry.Key, entry.Size); ;
                            keyName = entry.Key;
                            bufferSize = entry.Size;
                        }
                        //End of get the Object
                    } while (ListObjectsResponse.IsTruncated);
                }
                catch (AmazonS3Exception amazonS3Exception)
                {
                    Console.WriteLine("S3 error occurred. Exception: " + amazonS3Exception.ToString());
                    Console.ReadKey();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.ToString());
                    Console.ReadKey();
                }

                Console.Write("Enter File ID to download or 0 to continue without Downloading:  ");
                int numberOfFilesInBucket = ListObjectsResponse.S3Objects.Count;

                // Get a value from the user to select a downloadable file
                int downloadFileIndex = Convert.ToInt32(Console.ReadLine());
                while (downloadFileIndex < 0 || downloadFileIndex > numberOfFilesInBucket)
                {
                    Console.Write("Enter File ID to download or 0 to continue without Downloading:  ");
                    downloadFileIndex = Convert.ToInt32(Console.ReadLine());
                }

                if (downloadFileIndex > 0)
                {
                    //valid File Index

                    List<S3Object> FileObjects = ListObjectsResponse.S3Objects;

                    try
                    {
                        GetObjectRequest objectRequest = new GetObjectRequest
                        {
                            BucketName = activeBucket.BucketName,
                            Key = FileObjects[downloadFileIndex - 1].Key
                        };
                        using (GetObjectResponse objectResponse = await client.GetObjectAsync(objectRequest))
                        using (Stream responseStream = objectResponse.ResponseStream)
                        using (BinaryReader reader = new BinaryReader(responseStream))
                        {
                            using (FileStream fileStream = new FileStream(FileObjects[downloadFileIndex - 1].Key, FileMode.Create))
                            {
                                byte[] chunk;
                                chunk = reader.ReadBytes(1024);
                                while (chunk.Length > 0)
                                {
                                    foreach (byte segment in chunk)
                                        fileStream.WriteByte(segment);
                                    chunk = reader.ReadBytes(1024);
                                }
                            }

                        }
                    }
                    catch (AmazonS3Exception e)
                    {
                        Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object", e.Message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception when getting the list of buckets:");
                Console.WriteLine(e.Message);
            }

            //Upload a file 
            try
            {
                Console.Write("Upload Default Test file? (Y/N): ");
                string input = Console.ReadLine();
                input = input.Trim().ToLower();


                string uploadFilePath = "Scroll.jpg";
                string formatedDate = DateTime.Now.ToString();
                formatedDate = Regex.Replace(formatedDate, "[^a-zA-Z0-9]", "_");
                string fileName = "Scroll" + formatedDate + ".jpg";

                if (input.Equals("y") == true)
                {
                    Console.Write("Uploading the Ranger Scroll image...");
                }
                else if (input.Equals("n") == true)
                {
                    Console.Write("Enter a valid filepath: ");
                    uploadFilePath = Console.ReadLine();
                    char[] delimiterChars = { '\\', '/' };
                    string[] pathSplit = uploadFilePath.Split(delimiterChars);
                    fileName = pathSplit[pathSplit.Length - 1];
                }
                else
                {
                    Console.Write("Input Error");
                    return;
                }

                var putRequest1 = new PutObjectRequest
                {
                    BucketName = activeBucket.BucketName,
                    Key = fileName,
                    FilePath = uploadFilePath
                };

                PutObjectResponse response1 = await client.PutObjectAsync(putRequest1);

            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(
                        "Error encountered ***. Message:'{0}' when writing an object"
                        , e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Unknown encountered on server. Message:'{0}' when writing an object"
                    , e.Message);
            }
        }
    }
}


