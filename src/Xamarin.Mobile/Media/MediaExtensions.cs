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
   internal static class MediaExtensions
   {
      internal static String GetFilePath( this StoreMediaOptions self, String rootPath )
      {
         Boolean isPhoto = !(self is StoreVideoOptions);

         String name = (self != null) ? self.Name : null;
         if(String.IsNullOrWhiteSpace( name ))
         {
            String timestamp = DateTime.Now.ToString( "yyyyMMdd_HHmmss" );
            if(isPhoto)
            {
               name = "IMG_" + timestamp + ".jpg";
            }
            else
            {
               name = "VID_" + timestamp + ".mp4";
            }
         }

         String ext = Path.GetExtension( name );
         if(ext == String.Empty)
         {
            ext = ((isPhoto) ? ".jpg" : ".mp4");
         }

         name = Path.GetFileNameWithoutExtension( name );

         String folder = Path.Combine(
            rootPath ?? String.Empty,
            (self != null && self.Directory != null) ? self.Directory : String.Empty );

         return Path.Combine( folder, name + ext );
      }

      internal static String GetUniqueFilepath( this StoreMediaOptions self, String rootPath,
                                                Func<String, Boolean> checkExists )
      {
         String path = self.GetFilePath( rootPath );
         String folder = Path.GetDirectoryName( path );
         String ext = Path.GetExtension( path );
         String name = Path.GetFileNameWithoutExtension( path );

         String nname = name + ext;
         Int32 i = 1;
         while(checkExists( Path.Combine( folder, nname ) ))
         {
            nname = name + "_" + (i++) + ext;
         }

         return Path.Combine( folder, nname );
      }

      internal static void VerifyOptions( this StoreMediaOptions self )
      {
         if(self == null)
         {
            throw new ArgumentNullException( "options" );
         }
         //if (!Enum.IsDefined (typeof(MediaFileStoreLocation), options.Location))
         //    throw new ArgumentException ("options.Location is not a member of MediaFileStoreLocation");
         //if (options.Location == MediaFileStoreLocation.Local)
         //{
         //if (String.IsNullOrWhiteSpace (options.Directory))
         //	throw new ArgumentNullException ("options", "For local storage, options.Directory must be set");
         if(Path.IsPathRooted( self.Directory ))
         {
            throw new ArgumentException( "options.Directory must be a relative path", "options" );
         }
         //}
      }
   }
}