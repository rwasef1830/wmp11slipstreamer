using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace Epsilon.Security.Cryptography
{
    /// <summary>
    /// This is the exception class that is thrown throughout the Decryption process
    /// </summary>
    public class CryptoHelpException : Exception
    {
        public CryptoHelpException(string msg) : base(msg) { }
    }

    /// <summary>
    /// Summary description for CryptoHelp.
    /// </summary>
    public class CryptoHelp
    {
        /// <summary>
        /// Tag to make sure this file 
        /// is readable/decryptable by this class
        /// </summary>
        ulong _uniqueTag; // was 0xFC010203040506CF

        /// <summary>
        /// The amount of bytes to read from the file
        /// </summary>
        const int c_bufferSize = 16 * 1024;

        /// <summary>
        /// Initialises a new instance of the CryptoHelp class
        /// </summary>
        /// <param name="uniqueTag">A value that makes data encrypted or decrypted usable 
        /// only by an instance of this class having this same value.</param>
        public CryptoHelp(ulong uniqueTag)
        {
            this._uniqueTag = uniqueTag;
        }

        /// <summary>
        /// Checks to see if two byte array are equal
        /// </summary>
        /// <param name="b1">the first byte array</param>
        /// <param name="b2">the second byte array</param>
        /// <returns>true if b1.Length == b2.Length and each byte in b1 is
        /// equal to the corresponding byte in b2</returns>
        public static bool CheckByteArrays(byte[] b1, byte[] b2)
        {
            if (b1.Length == b2.Length)
            {
                for (int i = 0; i < b1.Length; ++i)
                    if (b1[i] != b2[i])
                        return false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a Rijndael SymmetricAlgorithm for use in EncryptFile and DecryptFile
        /// </summary>
        /// <param name="password">the string to use as the password</param>
        /// <param name="salt">the salt to use with the password</param>
        /// <returns>A SymmetricAlgorithm for encrypting/decrypting with Rijndael</returns>
        static SymmetricAlgorithm CreateRijndael(string password, byte[] salt)
        {
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, salt, 
                "SHA256", 1000);

            SymmetricAlgorithm sma = Rijndael.Create();
            sma.KeySize = 256;
            sma.Key = pdb.GetBytes(32);
            sma.Padding = PaddingMode.PKCS7;
            return sma;
        }

        public string DecryptToString(byte[] bytes, string password,
            Encoding encoder)
        {
            return encoder.GetString(DecryptToMemory(bytes, password));
        }

        public void DecryptToFile(byte[] bytes, string password,
            string fileNameToSave)
        {
            MemoryStream inStream = new MemoryStream(bytes);
            FileStream outStream = File.OpenWrite(fileNameToSave);
            DecryptStream(inStream, password, outStream, true);
            inStream.Close();
        }

        public byte[] DecryptToMemory(byte[] bytes, string password)
        {
            MemoryStream bytesStream = new MemoryStream(bytes);
            MemoryStream outputStream = new MemoryStream(bytes.Length);
            DecryptStream(bytesStream, password, outputStream, true);
            return outputStream.ToArray();
        }

        /// <summary>
        /// Takes an input stream and decrypts it to a string
        /// </summary>
        /// <param name="fin">input stream</param>
        /// <param name="fout">output stream</param>
        /// <param name="password">password</param>
        /// <param name="closeOutStream">true to close the output stream when decryption
        /// is completed.</param>
        public void DecryptStream(Stream inStream, 
            string password, Stream outStream, bool closeOutStream)
        {
            byte[] bytes = new byte[c_bufferSize]; // byte buffer
            int read = -1; // the amount of bytes read from the stream
            int value = 0;
            int outValue = 0; // the amount of bytes written out

            // read off the IV and Salt
            byte[] IV = new byte[16];
            inStream.Read(IV, 0, 16);
            byte[] salt = new byte[16];
            inStream.Read(salt, 0, 16);

            // create the crypting stream
            SymmetricAlgorithm sma = CryptoHelp.CreateRijndael(password, salt);
            sma.IV = IV;

            value = 32; // the value for the progress
            long lSize = -1; // the size stored in the input stream

            // create the hashing object, so that we can verify the file
            HashAlgorithm hasher = SHA256.Create();

            // create the cryptostreams that will process the file
            CryptoStream cin = new CryptoStream(inStream, sma.CreateDecryptor(),
                CryptoStreamMode.Read);
            CryptoStream chash 
                = new CryptoStream(Stream.Null, hasher, CryptoStreamMode.Write);

            // read size from file
            BinaryReader br = new BinaryReader(cin);
            lSize = br.ReadInt64();
            ulong tag = br.ReadUInt64();

            if (this._uniqueTag != tag)
                throw new CryptoHelpException("Stream corrupted!");

            //determine number of reads to process on the file
            long numReads = lSize / c_bufferSize;

            // determine what is left of the file, after numReads
            long slack = (long)lSize % c_bufferSize;

            // read the buffer_sized chunks
            for (int i = 0; i < numReads; ++i)
            {
                read = cin.Read(bytes, 0, bytes.Length);
                outStream.Write(bytes, 0, read);
                chash.Write(bytes, 0, read);
                value += read;
                outValue += read;
            }

            // now read the slack
            if (slack > 0)
            {
                read = cin.Read(bytes, 0, (int)slack);
                outStream.Write(bytes, 0, read);
                chash.Write(bytes, 0, read);
                value += read;
                outValue += read;
            }
            // flush and close the hashing stream
            chash.Flush();
            chash.Close();

            // flush and close the output file
            outStream.Flush();
            if (closeOutStream) outStream.Close();

            // read the current hash value
            byte[] curHash = hasher.Hash;

            // get and compare the current and old hash values
            byte[] oldHash = new byte[hasher.HashSize / 8];
            read = cin.Read(oldHash, 0, oldHash.Length);

            if ((oldHash.Length != read) || (!CheckByteArrays(oldHash, curHash)))
                throw new CryptoHelpException("Stream corrupted!");

            // make sure the written and stored size are equal
            if (outValue != lSize)
                throw new CryptoHelpException("Stream sizes don't match!");
        }
    }
}
