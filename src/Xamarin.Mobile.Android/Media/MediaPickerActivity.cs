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
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Environment = Android.OS.Environment;
using FileNotFoundException = Java.IO.FileNotFoundException;
using Uri = Android.Net.Uri;

namespace Xamarin.Media
{
   [Activity]
   internal class MediaPickerActivity : Activity
   {
      internal const string ExtraAction = "action";
      internal const string ExtraId = "id";
      internal const string ExtraLocation = "location";
      internal const string ExtraPath = "path";
      internal const string ExtraTasked = "tasked";
      internal const string ExtraType = "type";
      private string action;

      private string description;
      private int id;
      private bool isPhoto;
      /// <summary>
      /// The user's destination path.
      /// </summary>
      private Uri path;
      private VideoQuality quality;
      private int seconds;

      private bool tasked;
      private string title;
      private string type;

      internal static event EventHandler<MediaPickedEventArgs> MediaPicked;

      protected override void OnActivityResult( int requestCode, Result resultCode, Intent data )
      {
         base.OnActivityResult( requestCode, resultCode, data );

         if(tasked)
         {
            Task<MediaPickedEventArgs> future;

            if(resultCode == Result.Canceled)
            {
               future = TaskFromResult( new MediaPickedEventArgs( requestCode, isCanceled: true ) );
            }
            else
            {
               future = GetMediaFileAsync(
                  this,
                  requestCode,
                  action,
                  isPhoto,
                  ref path,
                  (data != null) ? data.Data : null );
            }

            Finish();

            future.ContinueWith( t => OnMediaPicked( t.Result ) );
         }
         else
         {
            if(resultCode == Result.Canceled)
            {
               SetResult( Result.Canceled );
            }
            else
            {
               Intent resultData = new Intent();
               resultData.PutExtra( MediaFile.ExtraName, (data != null) ? data.Data : null );
               resultData.PutExtra( "path", path );
               resultData.PutExtra( "isPhoto", isPhoto );
               resultData.PutExtra( "action", action );

               SetResult( Result.Ok, resultData );
            }

            Finish();
         }
      }

      protected override void OnCreate( Bundle savedInstanceState )
      {
         base.OnCreate( savedInstanceState );

         Bundle b = (savedInstanceState ?? Intent.Extras);

         bool ran = b.GetBoolean( "ran", defaultValue: false );

         title = b.GetString( MediaStore.MediaColumns.Title );
         description = b.GetString( MediaStore.Images.ImageColumns.Description );

         tasked = b.GetBoolean( ExtraTasked );
         id = b.GetInt( ExtraId, 0 );
         type = b.GetString( ExtraType );
         if(type == "image/*")
         {
            isPhoto = true;
         }

         action = b.GetString( ExtraAction );
         Intent pickIntent = null;
         try
         {
            pickIntent = new Intent( action );
            if(action == Intent.ActionPick)
            {
               pickIntent.SetType( type );
            }
            else
            {
               if(!isPhoto)
               {
                  seconds = b.GetInt( MediaStore.ExtraDurationLimit, 0 );
                  if(seconds != 0)
                  {
                     pickIntent.PutExtra( MediaStore.ExtraDurationLimit, seconds );
                  }
               }

               quality = (VideoQuality)b.GetInt( MediaStore.ExtraVideoQuality, (int)VideoQuality.High );
               pickIntent.PutExtra( MediaStore.ExtraVideoQuality, GetVideoQuality( quality ) );

               if(!ran)
               {
                  path = GetOutputMediaFile( this, b.GetString( ExtraPath ), title, isPhoto );

                  Touch();
                  pickIntent.PutExtra( MediaStore.ExtraOutput, path );
               }
               else
               {
                  path = Uri.Parse( b.GetString( ExtraPath ) );
               }
            }

            if(!ran)
            {
               StartActivityForResult( pickIntent, id );
            }
         }
         catch(Exception ex)
         {
            OnMediaPicked( new MediaPickedEventArgs( id, ex ) );
         }
         finally
         {
            if(pickIntent != null)
            {
               pickIntent.Dispose();
            }
         }
      }

      protected override void OnSaveInstanceState( Bundle outState )
      {
         outState.PutBoolean( "ran", true );
         outState.PutString( MediaStore.MediaColumns.Title, title );
         outState.PutString( MediaStore.Images.ImageColumns.Description, description );
         outState.PutInt( ExtraId, id );
         outState.PutString( ExtraType, type );
         outState.PutString( ExtraAction, action );
         outState.PutInt( MediaStore.ExtraDurationLimit, seconds );
         outState.PutInt( MediaStore.ExtraVideoQuality, (int)quality );
         outState.PutBoolean( ExtraTasked, tasked );

         if(path != null)
         {
            outState.PutString( ExtraPath, path.Path );
         }

         base.OnSaveInstanceState( outState );
      }

      private void Touch()
      {
         if(path.Scheme != "file")
         {
            return;
         }

         File.Create( GetLocalPath( path ) ).Close();
      }

      internal static Task<Tuple<string, bool>> GetFileForUriAsync( Context context, Uri uri, bool isPhoto )
      {
         var tcs = new TaskCompletionSource<Tuple<string, bool>>();

         if(uri.Scheme == "file")
         {
            tcs.SetResult( new Tuple<string, bool>( new System.Uri( uri.ToString() ).LocalPath, false ) );
         }
         else if(uri.Scheme == "content")
         {
            Task.Factory.StartNew(
               () =>
               {
                  ICursor cursor = null;
                  try
                  {
                     cursor = context.ContentResolver.Query( uri, null, null, null, null );
                     if(cursor == null || !cursor.MoveToNext())
                     {
                        tcs.SetResult( new Tuple<string, bool>( null, false ) );
                     }
                     else
                     {
                        int column = cursor.GetColumnIndex( MediaStore.MediaColumns.Data );
                        string contentPath = null;

                        if(column != -1)
                        {
                           contentPath = cursor.GetString( column );
                        }

                        bool copied = false;

                        // If they don't follow the "rules", try to copy the file locally
                        if(contentPath == null || !contentPath.StartsWith( "file" ))
                        {
                           copied = true;
                           Uri outputPath = GetOutputMediaFile( context, "temp", null, isPhoto );

                           try
                           {
                              using(Stream input = context.ContentResolver.OpenInputStream( uri )) using(Stream output = File.Create( outputPath.Path )) input.CopyTo( output );

                              contentPath = outputPath.Path;
                           }
                           catch(FileNotFoundException)
                           {
                              // If there's no data associated with the uri, we don't know
                              // how to open this. contentPath will be null which will trigger
                              // MediaFileNotFoundException.
                           }
                        }

                        tcs.SetResult( new Tuple<string, bool>( contentPath, copied ) );
                     }
                  }
                  finally
                  {
                     if(cursor != null)
                     {
                        cursor.Close();
                        cursor.Dispose();
                     }
                  }
               },
               CancellationToken.None,
               TaskCreationOptions.None,
               TaskScheduler.Default );
         }
         else
         {
            tcs.SetResult( new Tuple<string, bool>( null, false ) );
         }

         return tcs.Task;
      }

      internal static Task<MediaPickedEventArgs> GetMediaFileAsync( Context context, int requestCode, string action,
                                                                    bool isPhoto, ref Uri path, Uri data )
      {
         Task<Tuple<string, bool>> pathFuture;

         string originalPath = null;

         if(action != Intent.ActionPick)
         {
            originalPath = path.Path;

            // Not all camera apps respect EXTRA_OUTPUT, some will instead
            // return a content or file uri from data.
            if(data != null && data.Path != originalPath)
            {
               originalPath = data.ToString();
               string currentPath = path.Path;
               pathFuture =
                  TryMoveFileAsync( context, data, path, isPhoto )
                     .ContinueWith( t => new Tuple<string, bool>( t.Result ? currentPath : null, false ) );
            }
            else
            {
               pathFuture = TaskFromResult( new Tuple<string, bool>( path.Path, false ) );
            }
         }
         else if(data != null)
         {
            originalPath = data.ToString();
            path = data;
            pathFuture = GetFileForUriAsync( context, path, isPhoto );
         }
         else
         {
            pathFuture = TaskFromResult<Tuple<string, bool>>( null );
         }

         return pathFuture.ContinueWith(
            t =>
            {
               string resultPath = t.Result.Item1;
               if(resultPath != null && File.Exists( t.Result.Item1 ))
               {
                  var mf = new MediaFile( resultPath, deletePathOnDispose: t.Result.Item2 );
                  return new MediaPickedEventArgs( requestCode, false, mf );
               }
               else
               {
                  return new MediaPickedEventArgs( requestCode, new MediaFileNotFoundException( originalPath ) );
               }
            } );
      }

      private static string GetLocalPath( Uri uri )
      {
         return new System.Uri( uri.ToString() ).LocalPath;
      }

      private static Uri GetOutputMediaFile( Context context, string subdir, string name, bool isPhoto )
      {
         subdir = subdir ?? String.Empty;

         if(String.IsNullOrWhiteSpace( name ))
         {
            string timestamp = DateTime.Now.ToString( "yyyyMMdd_HHmmss" );
            if(isPhoto)
            {
               name = "IMG_" + timestamp + ".jpg";
            }
            else
            {
               name = "VID_" + timestamp + ".mp4";
            }
         }

         string mediaType = (isPhoto) ? Environment.DirectoryPictures : Environment.DirectoryMovies;
         using(Java.IO.File mediaStorageDir = new Java.IO.File( context.GetExternalFilesDir( mediaType ), subdir ))
         {
            if(!mediaStorageDir.Exists())
            {
               if(!mediaStorageDir.Mkdirs())
               {
                  throw new IOException(
                     "Couldn't create directory, have you added the WRITE_EXTERNAL_STORAGE permission?" );
               }

               // Ensure this media doesn't show up in gallery apps
               using(Java.IO.File nomedia = new Java.IO.File( mediaStorageDir, ".nomedia" )) nomedia.CreateNewFile();
            }

            return Uri.FromFile( new Java.IO.File( GetUniquePath( mediaStorageDir.Path, name, isPhoto ) ) );
         }
      }

      private static string GetUniquePath( string folder, string name, bool isPhoto )
      {
         string ext = Path.GetExtension( name );
         if(ext == String.Empty)
         {
            ext = ((isPhoto) ? ".jpg" : ".mp4");
         }

         name = Path.GetFileNameWithoutExtension( name );

         string nname = name + ext;
         int i = 1;
         while(File.Exists( Path.Combine( folder, nname ) ))
         {
            nname = name + "_" + (i++) + ext;
         }

         return Path.Combine( folder, nname );
      }

      private static int GetVideoQuality( VideoQuality videoQuality )
      {
         switch(videoQuality)
         {
            case VideoQuality.Medium:
            case VideoQuality.High:
               return 1;

            default:
               return 0;
         }
      }

      private static void OnMediaPicked( MediaPickedEventArgs e )
      {
         var picked = MediaPicked;
         if(picked != null)
         {
            picked( null, e );
         }
      }

      private static Task<T> TaskFromResult<T>( T result )
      {
         var tcs = new TaskCompletionSource<T>();
         tcs.SetResult( result );
         return tcs.Task;
      }

      private static Task<bool> TryMoveFileAsync( Context context, Uri url, Uri path, bool isPhoto )
      {
         string moveTo = GetLocalPath( path );
         return GetFileForUriAsync( context, url, isPhoto ).ContinueWith(
            t =>
            {
               if(t.Result.Item1 == null)
               {
                  return false;
               }

               File.Delete( moveTo );
               File.Move( t.Result.Item1, moveTo );

               if(url.Scheme == "content")
               {
                  context.ContentResolver.Delete( url, null, null );
               }

               return true;
            },
            TaskScheduler.Default );
      }
   }

   internal class MediaPickedEventArgs : EventArgs
   {
      public MediaPickedEventArgs( int id, Exception error )
      {
         if(error == null)
         {
            throw new ArgumentNullException( "error" );
         }

         RequestId = id;
         Error = error;
      }

      public MediaPickedEventArgs( int id, bool isCanceled, MediaFile media = null )
      {
         RequestId = id;
         IsCanceled = isCanceled;
         if(!IsCanceled && media == null)
         {
            throw new ArgumentNullException( "media" );
         }

         Media = media;
      }

      public Exception Error { get; private set; }

      public bool IsCanceled { get; private set; }

      public MediaFile Media { get; private set; }

      public int RequestId { get; private set; }

      public Task<MediaFile> ToTask()
      {
         var tcs = new TaskCompletionSource<MediaFile>();

         if(IsCanceled)
         {
            tcs.SetCanceled();
         }
         else if(Error != null)
         {
            tcs.SetException( Error );
         }
         else
         {
            tcs.SetResult( Media );
         }

         return tcs.Task;
      }
   }
}