//
//  Copyright 2011-2013, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.IO;

namespace Xamarin.Media
{
   public sealed class MediaFile
      : IDisposable,
        IMediaFile
   {
      private readonly Action<Boolean> dispose;
      private readonly String path;
      private readonly Func<Stream> streamGetter;
      private Boolean isDisposed;

      public MediaFile( String path, Func<Stream> streamGetter, Action<Boolean> dispose = null )
      {
         // ctor was previously internal, not sure why...
         this.dispose = dispose;
         this.streamGetter = streamGetter;
         this.path = path;
      }

      public String Path
      {
         get
         {
            if(isDisposed)
            {
               throw new ObjectDisposedException( null );
            }

            return path;
         }
      }

      public void Dispose()
      {
         Dispose( true );
         GC.SuppressFinalize( this );
      }

      public Stream GetStream()
      {
         if(isDisposed)
         {
            throw new ObjectDisposedException( null );
         }

         return streamGetter();
      }

      private void Dispose( Boolean disposing )
      {
         if(isDisposed)
         {
            return;
         }

         isDisposed = true;
         if(dispose != null)
         {
            dispose( disposing );
         }
      }

      ~MediaFile()
      {
         Dispose( false );
      }
   }
}