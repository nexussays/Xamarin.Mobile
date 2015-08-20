using System;
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
}