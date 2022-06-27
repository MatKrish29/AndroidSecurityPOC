using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Android.Security.Keystore;
using Java.Security;
using Javax.Crypto;
using System.IO;
using System.Text;
using AndroidX.Core.Content;
using Android;
using Javax.Crypto.Spec;
using Java.IO;
using Android.Content;
using System.Linq;
using Console = System.Console;

// Just a POC for testing encryption/decryption mechanism

namespace AndroidSecurityPOC
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private static readonly string KEY_NAME = "com.xamarin.android.sample.encryption_key";
        private static readonly string KEYSTORE_NAME = "AndroidKeyStore";
        private static readonly string KEY_ALGORITHM = KeyProperties.KeyAlgorithmAes;
        private static readonly string BLOCK_MODE = KeyProperties.BlockModeGcm;
        private static readonly string ENCRYPTION_PADDING = KeyProperties.EncryptionPaddingNone;
        private static readonly string TRANSFORMATION = KEY_ALGORITHM + "/" + BLOCK_MODE + "/" + ENCRYPTION_PADDING;

        private KeyStore _keystore;

        private readonly string TO_ENCRYPT = "This is an encryption test";

        private byte[] store;
        private byte[] iv;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            _keystore = KeyStore.GetInstance(KEYSTORE_NAME);
            _keystore.Load(null);

            EncryptData();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void EncryptData()
        {
            byte[] encryptedData = GetEncryptedData(Encoding.ASCII.GetBytes(TO_ENCRYPT));
            store = encryptedData;

            Console.WriteLine("Encrypted: " + Convert.ToBase64String(encryptedData));

            DecryptData();
        }

        private void DecryptData()
        {
            byte[] decryptedData = GetDecryptedData(store);
            Console.WriteLine("Decrypted: " + Encoding.ASCII.GetString(decryptedData));
        }

        private byte[] GetEncryptedData(byte[] dataToEncrypt)
        {
            IKey key = GetKey();
            Cipher cipher = Cipher.GetInstance(TRANSFORMATION);

            try
            {
                cipher.Init(CipherMode.EncryptMode, key);
                iv = cipher.GetIV();
                byte[] encryptedData = cipher.DoFinal(dataToEncrypt);

                return encryptedData;
            }
            catch
            {
                return new byte[0];
            }
        }

        private byte[] GetDecryptedData(byte[] dataToDecrypt)
        {
            IKey key = GetKey();
            Cipher cipher = Cipher.GetInstance(TRANSFORMATION);

            try
            {
                GCMParameterSpec parameterSpec = new GCMParameterSpec(128, iv);
                cipher.Init(CipherMode.DecryptMode, key, parameterSpec);
                byte[] decryptedData = cipher.DoFinal(dataToDecrypt);

                return decryptedData;
            }
            catch
            {
                return new byte[0];
            }
        }

        private IKey GetKey()
        {
            IKey secretKey;

            if (!_keystore.IsKeyEntry(KEY_NAME))
            {
                CreateKey();
            }

            secretKey = _keystore.GetKey(KEY_NAME, null);
            return secretKey;
        }

        private void CreateKey()
        {
            KeyGenerator keyGen = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, KEYSTORE_NAME);
            KeyGenParameterSpec keyGenSpec =
                new KeyGenParameterSpec.Builder(KEY_NAME, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                    .SetBlockModes(BLOCK_MODE)
                    .SetEncryptionPaddings(ENCRYPTION_PADDING)
                    .SetRandomizedEncryptionRequired(true)
                    .Build();
            keyGen.Init(keyGenSpec);
            keyGen.GenerateKey();
        }

        public static bool IsApplicationSentToBackground(Context context)
        {
            ActivityManager am = (ActivityManager)context.GetSystemService(Context.ActivityService);
            if (am.GetRunningTasks(1).Any())
            {
                ComponentName topActivity = am.GetRunningTasks(1)[0].TopActivity;
                if (topActivity.PackageName != (context.PackageName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
