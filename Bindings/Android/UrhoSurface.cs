using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Org.Libsdl.App;
using Urho;

namespace Urho.Droid
{

	/// <summary>
	/// A controller that provides a SDLSurface that can be used in any activity.
	/// Make sure you handle these events in your Activity:
	/// - OnResume
	/// - OnPause
	/// - OnLowMemory
	/// - OnDestroy
	/// - DispatchKeyEvent
	/// - OnWindowFocusChanged
	/// </summary>
	public class UrhoSurface
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int SdlCallback(IntPtr context);

		[DllImport(Consts.NativeImport, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void RegisterSdlLauncher(SdlCallback callback);

		public static bool IsAlive => SDLActivity.MIsSurfaceReady;

		/// <summary>
		/// Creates a view (SurfaceView) that can be added anywhere
		/// </summary>
		public static UrhoSurfacePlaceholder CreateSurface(Activity activity)
		{
			var placeholder = new UrhoSurfacePlaceholder(activity) {
					LayoutParameters = new ViewGroup.LayoutParams (
						ViewGroup.LayoutParams.MatchParent,
						ViewGroup.LayoutParams.MatchParent)
				};
			return placeholder;
		}

		public static void OnResume()
		{
			SDLActivity.OnResume();
		}

		public static void OnPause()
		{
			SDLActivity.OnPause();
		}

		public static void OnLowMemory()
		{
			SDLActivity.OnLowMemory();
		}

		public static void OnDestroy()
		{
			SDLActivity.OnDestroy();
		}

		public static bool DispatchKeyEvent(KeyEvent keyEvent)
		{
			return SDLActivity.DispatchKeyEvent(keyEvent);
		}

		public static void OnWindowFocusChanged(bool focus)
		{
			SDLActivity.OnWindowFocusChanged(focus);
		}

		/// <summary>
		/// The simpliest way to launch a game. It opens a special full-screen activity
		/// </summary>
		public static void RunInActivity<TApplication>(ApplicationOptions options = null) where TApplication : Application
		{
			RunInActivity(typeof (TApplication), options);
		}

		/// <summary>
		/// The simpliest way to launch a game. It opens a special full-screen activity
		/// </summary>
		public static void RunInActivity(Type appType, ApplicationOptions options = null)
		{
			RunInActivity(() => Application.CreateInstance(appType, options));
		}

		/// <summary>
		/// The simpliest way to launch a game. It opens a special full-screen activity
		/// </summary>
		public static void RunInActivity(Func<Application> applicationFactory)
		{
			SetSdlMain(applicationFactory, true, null);
			var context = Android.App.Application.Context;
			var intent = new Intent(context, typeof(Org.Libsdl.App.UrhoActivity));
			intent.AddFlags(ActivityFlags.NewTask);
			context.StartActivity(intent);
		}

		internal static void SetSdlMain(Func<Application> applicationFactory, bool finishActivityOnExit, SDLSurface surface)
		{
			SDLActivity.FinishActivityOnNativeExit = finishActivityOnExit;
			RegisterSdlLauncher(_ => {
					var app = applicationFactory();
					app.UrhoSurface = surface;
					var code = app.Run();
					Log.Warn("URHOSHARP", "App exited: " + code);
					return code;
				});
		}
	}

	public class UrhoSurfacePlaceholder : FrameLayout
	{
		public SDLSurface SDLSurface;

		public UrhoSurfacePlaceholder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {}
		public UrhoSurfacePlaceholder(Android.Content.Context context) : base(context) {}
		public UrhoSurfacePlaceholder(Android.Content.Context context, IAttributeSet attrs) : base(context, attrs) {}
		public UrhoSurfacePlaceholder(Android.Content.Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) {}
		public UrhoSurfacePlaceholder(Android.Content.Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) {}

		public async Task<Application> Show(Type appType, ApplicationOptions options = null, bool finishActivityOnExit = false)
		{
			await Stop();
			if (SDLSurface != null)
			{
				RemoveView(SDLSurface);
			}

			SDLSurface = SDLActivity.CreateSurface(Context as Activity);
			AddView(SDLSurface, ViewGroup.LayoutParams.MatchParent);

			var tcs = new TaskCompletionSource<Application>();
			Action startedHandler = null;
			startedHandler = () =>
				{
					Application.Started -= startedHandler;
					tcs.TrySetResult(Application.Current);
				};

			Application.Started += startedHandler;
			UrhoSurface.SetSdlMain(() => Application.CreateInstance(appType, options), finishActivityOnExit, SDLSurface);
			return await tcs.Task;
		}

		public async Task<TApplication> Show<TApplication>(ApplicationOptions options = null,
			bool finishActivityOnExit = false)
			where TApplication : Application
		{
			var app = await Show(typeof(TApplication), options, finishActivityOnExit);
			return (TApplication)app;
		}

		public async Task Stop()
		{
			if (Application.HasCurrent && Application.Current.IsActive)
				await Application.Current.Exit();
		}
	}
}