using System;
using System.IO;

namespace Xamarin.Media
{
   public interface IMediaFile
   {
      String Path { get; }

      void Dispose();

      Stream GetStream();
   }
}