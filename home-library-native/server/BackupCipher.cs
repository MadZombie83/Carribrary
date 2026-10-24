using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
namespace HomeLibrary;
// Authenticated streaming frames include an authenticated terminal record, so
// modified headers, reordering, truncation and appended bytes are rejected.
public static class BackupCipher {
 const int Chunk=65536,Iterations=600000;static readonly byte[] Magic="CARRBK01"u8.ToArray();
 public static void Password(string value){if(string.IsNullOrWhiteSpace(value)||value.Length is <12 or >128||value.Any(char.IsControl))throw new LibraryError("Choose a backup password of 12–128 characters. Keep it safely; it cannot be reset.");}
 public static void Encrypt(Stream plain,Stream output,string password,CancellationToken stop=default){
  Password(password);var header=new byte[40];Magic.CopyTo(header,0);RandomNumberGenerator.Fill(header.AsSpan(8,24));BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(32),Chunk);BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(36),Iterations);output.Write(header);
  var key=Rfc2898DeriveBytes.Pbkdf2(password,header.AsSpan(8,16),Iterations,HashAlgorithmName.SHA256,32);var input=new byte[Chunk];var cipher=new byte[Chunk];
  try{using var aes=new AesGcm(key,16);uint index=0;while(true){stop.ThrowIfCancellationRequested();var count=ReadChunk(plain,input);var nonce=Nonce(header,index);var aad=Aad(header,index,count);var tag=new byte[16];aes.Encrypt(nonce,input.AsSpan(0,count),cipher.AsSpan(0,count),tag,aad);var length=new byte[4];BinaryPrimitives.WriteInt32LittleEndian(length,count);output.Write(length);output.Write(cipher,0,count);output.Write(tag);if(count==0)break;index=checked(index+1);}}
  finally{CryptographicOperations.ZeroMemory(key);CryptographicOperations.ZeroMemory(input);}
 }
 public static void Decrypt(Stream input,Stream plain,string password,CancellationToken stop=default){
  Password(password);var header=new byte[40];try{input.ReadExactly(header);}catch(EndOfStreamException){throw new LibraryError("The complete backup is truncated or damaged.");}if(!header.AsSpan(0,8).SequenceEqual(Magic)||BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(32))!=Chunk||BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(36))!=Iterations)throw new LibraryError("This is not a supported complete Carribrary backup.");
  var key=Rfc2898DeriveBytes.Pbkdf2(password,header.AsSpan(8,16),Iterations,HashAlgorithmName.SHA256,32);var cipher=new byte[Chunk];var output=new byte[Chunk];long total=0;
  try{using var aes=new AesGcm(key,16);uint index=0;while(true){stop.ThrowIfCancellationRequested();var length=new byte[4];input.ReadExactly(length);var count=BinaryPrimitives.ReadInt32LittleEndian(length);if(count<0||count>Chunk)throw new LibraryError("The complete backup is damaged.");input.ReadExactly(cipher.AsSpan(0,count));var tag=new byte[16];input.ReadExactly(tag);aes.Decrypt(Nonce(header,index),cipher.AsSpan(0,count),tag,output.AsSpan(0,count),Aad(header,index,count));if(count==0){if(input.ReadByte()!=-1)throw new LibraryError("The backup contains unexpected trailing data.");break;}total+=count;if(total>CompleteBackup.MaxArchiveBytes)throw new LibraryError("The backup exceeds the supported recovery size.");plain.Write(output,0,count);index=checked(index+1);}}
  catch(CryptographicException){throw new LibraryError("The backup password is incorrect or the file is damaged.");}
  catch(EndOfStreamException){throw new LibraryError("The complete backup is truncated or damaged.");}
  finally{CryptographicOperations.ZeroMemory(key);CryptographicOperations.ZeroMemory(output);}
 }
 static int ReadChunk(Stream stream,byte[] buffer){var n=0;while(n<buffer.Length){var got=stream.Read(buffer,n,buffer.Length-n);if(got==0)break;n+=got;}return n;}
 static byte[] Nonce(byte[] header,uint index){var result=new byte[12];header.AsSpan(24,8).CopyTo(result);BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(8),index);return result;}
 static byte[] Aad(byte[] header,uint index,int count){var result=new byte[48];header.CopyTo(result,0);BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(40),index);BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(44),count);return result;}
}
