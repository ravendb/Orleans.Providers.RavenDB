using System.Buffers;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.WireProtocol;

namespace Orleans.Providers.RavenDB;

[RegisterSerializer]
public sealed class ConcurrencyExceptionCodec : IFieldCodec<Raven.Client.Exceptions.ConcurrencyException>
{
    public ConcurrencyExceptionCodec()
    {
        Console.WriteLine("ConcurrencyExceptionCodec successfully registered!");
    }

    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, Raven.Client.Exceptions.ConcurrencyException value)
        where TBufferWriter : IBufferWriter<byte>
    {
        ReferenceCodec.MarkValueField(writer.Session);
        writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(Raven.Client.Exceptions.ConcurrencyException), WireType.LengthPrefixed);

        StringCodec.WriteField(ref writer, 0, value.Message); // Serialize the message
    }

    public Raven.Client.Exceptions.ConcurrencyException ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        ReferenceCodec.MarkValueField(reader.Session);

        var message = StringCodec.ReadValue(ref reader, field); // Deserialize the message
        return new Raven.Client.Exceptions.ConcurrencyException(message);
    }
}