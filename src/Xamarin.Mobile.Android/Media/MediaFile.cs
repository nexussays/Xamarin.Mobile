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
using System.Threading.Tasks;
using Android.Content;
using Uri = Android.Net.Uri;

namespace Xamarin.Media
{
   public static class MediaFileExtensions
   {
      public static Task<MediaFile> GetMediaFileExtraAsync( this Intent self, Context context )
      {
         if(self == null)
         {
            throw new ArgumentNullException( "self" );
         }
         if(context == null)
         {
            throw new ArgumentNullException( "context" );
         }

         String action = self.GetStringExtra( "action" );
         if(action == null)
         {
            throw new ArgumentException( "Intent was not results from MediaPicker", "self" );
         }

         var uri = (Uri)self.GetParcelableExtra( MediaFile.ExtraName );
         Boolean isPhoto = self.GetBooleanExtra( "isPhoto", false );
         var path = (Uri)self.GetParcelableExtra( "path" );

         return
            MediaPickerActivity.GetMediaFileAsync( context, 0, action, isPhoto, ref path, uri )
                               .ContinueWith( t => t.Result.ToTask() )
                               .Unwrap();
      }
   }

   public sealed class MediaFile : IDisposable
   {
      internal const String ExtraName = "MediaFile";
      private readonly Boolean deletePathOnDispose;
      private readonly String path;
      private Boolean isDisposed;

      internal MediaFile( String path, Boolean deletePathOnDispose )
      {
         this.deletePathOnDispose = deletePathOnDispose;
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

         return File.OpenRead( path );
      }

      private void Dispose( Boolean disposing )
      {
         if(isDisposed)
         {
            return;
         }

         isDisposed = true;
         if(deletePathOnDispose)
         {
            try
            {
               File.Delete( path );
               // We don't really care if this explodes for a normal IO reason.
            }
            catch(UnauthorizedAccessException)
            {
            }
            catch(DirectoryNotFoundException)
            {
            }
            catch(IOException)
            {
            }
         }
      }

      ~MediaFile()
      {
         Dispose( false );
      }
   }
}