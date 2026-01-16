namespace CSBackend;

public static class Preprocessor
{
    public static void PreprocessToCore(StreamReader @in, StreamWriter @out)
    {
        using MemoryStream sourceBuffer = new();
        using MemoryStream targetBuffer = new();

        // 1. Initial Pass: Copy raw input to sourceBuffer
        @in.BaseStream.CopyTo(sourceBuffer);

        void RunPass(Action<StreamReader, StreamWriter> passAction)
        {
            sourceBuffer.Position = 0;
            targetBuffer.SetLength(0);
        
            // We use 'using' here to ensure the Writer is FLUSHED before we copy
            using (var reader = new StreamReader(sourceBuffer, leaveOpen: true))
            using (var writer = new StreamWriter(targetBuffer, leaveOpen: true))
            {
                passAction(reader, writer);
                writer.Flush(); // Crucial: push remaining bits to targetBuffer
            }

            sourceBuffer.SetLength(0);
            targetBuffer.Position = 0;
            targetBuffer.CopyTo(sourceBuffer);
        }


        // 3. Final Pass: write sourceBuffer to output
        @out.Flush(); // Ensure output is ready
        sourceBuffer.Position = 0;
        sourceBuffer.CopyTo(@out.BaseStream);
        @out.Flush();
    }

    private static void ConvertNativeTypeKeyword(StreamReader @in, StreamWriter sw)
    {
        
    }
}