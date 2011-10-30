using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Epsilon.IO
{
    public class Streams
    {
        const int c_bufferSize = 32 * 1024;

        public static long Search(Stream stream, byte[] sequence)
        {
            return Search(stream, sequence, c_bufferSize);
        }

        public static long Search(Stream stream, byte[] sequence, int bufferSize)
        {
            if (!stream.CanRead || !stream.CanSeek)
                throw new InvalidOperationException(
                    "Stream must be readable and seekable.");
            if (sequence == null || sequence.Length == 0)
                throw new ArgumentException("Sequence array must not be null or empty.");

            // Save the position
            long initalPosition = stream.Position;
            
            // Initialise the buffer to an empty managed array, this assignment
            // should only ever happen once.
            byte[] buffer = new byte[bufferSize];

            // Counters
            long totalBytesRead = 0;
            int bytesRead = 0;
            int matchedBytes = 0;
            int seqIter = 0;

            // Must get match on first byte of new buffer
            bool lastLoopGotPartialMatch = false;

            // Loop reading through the buffer in bufferSize'd chunks until
            // we run out of data or until we've matched the entire sequence
            // in a previous loop.
            while (((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                && matchedBytes < sequence.Length)
            {
                totalBytesRead += bytesRead;

                // Before each loop, check against readBytes not length of buffer 
                // simply because when the last chunk is less than the default buffer 
                // size, we'll be unnecessarily reading in garbage (the rest of the 
                // array) and  waste time.
                for (int buffIter = 0; buffIter < bytesRead; buffIter++)
                {
                    byte bufferByte = buffer[buffIter];
                    byte seqByte = sequence[seqIter];

                    // First, try to match the first byte of the sequence and 
                    // then we'll work from there.
                    if (seqByte == bufferByte)
                    {
                        // Switch off the partial match flag
                        lastLoopGotPartialMatch = false;

                        // Don't forget that the first byte IS part of the 
                        // matched sequence.
                        matchedBytes++;

                        // Read more trying to match the rest of the sequence
                        // from that point in the buffer until we reach its end
                        // or until we've matched the entire sequence.
                        while (++seqIter < sequence.Length && ++buffIter < bytesRead)
                        {
                            seqByte = sequence[seqIter];
                            bufferByte = buffer[buffIter];

                            if (seqByte == bufferByte) 
                                matchedBytes++;
                            else
                            {
                                // A byte didn't match, just break now 
                                // and we'll handle this with the rest
                                // of the cases to maintain code consistency.
                                break;
                            }
                        }

                        // Yatta ze! We finally matched the entire sequence
                        if (matchedBytes == sequence.Length)
                        {
                            long matchPos = 
                                initalPosition + totalBytesRead 
                                - matchedBytes - (bytesRead - buffIter - 1);
                            stream.Seek(initalPosition, SeekOrigin.Begin);
                            return matchPos;
                        }
                        else if (buffIter < bytesRead)
                        {
                            // Hmmmm, looks like this wasn't a real match after all,
                            // a byte from the buffer didn't match so we aborted,
                            // so reset the matchedBytes count and the sequence iterator.
                            matchedBytes = seqIter = 0;

                            // Decrement the buffer iterator to repeat processing 
                            // this byte with the first byte of the sequence again.
                            buffIter--;

                            // Continue the loop
                            continue;
                        }
                        else
                        {
                            // We ran out of buffer before we could match the 
                            // rest of the sequence, need more data.

                            // This is to signal that on the next loop we must
                            // get a match right on the first byte of the buffer.
                            lastLoopGotPartialMatch = true;

                            // Continue and break have the same effect in this case, 
                            // but doing a break will save an extra bounds check.
                            break;
                        }
                    }
                    else if (lastLoopGotPartialMatch)
                    {
                        // We failed to complete the match after we
                        // read more data, reset the sequence iterator,
                        // matched bytes counter and decrement the buffer
                        // iterator to repeat this loop again.
                        //
                        // (Maybe this new buffer has at its start the entire sequence).
                        matchedBytes = seqIter = 0;
                        lastLoopGotPartialMatch = false;
                        buffIter--;
                        continue;
                    }
                } // Buffer iteration loop
            } // Read buffer loop

            // We should only reach here in case of failure
            Debug.Assert(matchedBytes != sequence.Length, 
                "Going to return -1 when we have a complete match.");

            // Restore the stream to its last position
            stream.Seek(initalPosition, SeekOrigin.Begin);
            return -1L;
        }
    }
}
