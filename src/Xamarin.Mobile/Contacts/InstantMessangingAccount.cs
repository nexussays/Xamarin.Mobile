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

namespace Xamarin.Contacts
{
   public enum InstantMessagingService
   {
      Aim,
      Msn,
      Yahoo,
      Icq,
      Jabber,
      Other,
      Skype,
      Facebook,
      Google,
      GaduGadu,
      QQ
   }

   public class InstantMessagingAccount
   {
      public string Account { get; set; }

      public InstantMessagingService Service { get; set; }

      public string ServiceLabel { get; set; }
   }
}