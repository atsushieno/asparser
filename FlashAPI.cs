using System;
using System.Collections.Generic;
using System.Linq;

using flash;
using flash.display;
using flash.errors;
using flash.events;
using flash.media;
using flash.net;
using flash.utils;

namespace flash
{
}

namespace flash.display
{
	public class DisplayObjectContainer
	{
	}

	public class Loader
	{
	}
	
	public class Sprite
	{
	}

	public class BitmapData
	{
	}
}

namespace flash.errors
{
}

namespace flash.events
{
	public class Event : global::Object
	{
		public virtual Event clone ()
		{
			throw new NotImplementedException ();
		}
	}

	public class ErrorEvent : Event
	{
	}

	public class IOErrorEvent : ErrorEvent
	{
	}
	public class ProgressEvent : Event
	{
	}
	
	public class SampleDataEvent : Event
	{
	}

	public class EventDispatcher : global::Object
	{
	}
}

namespace flash.media
{
	public class Sound
	{
	}
	
	public class SoundChannel : EventDispatcher
	{
	}
	
	public class SoundLoaderContext
	{
	}
	
	public class SoundTransform
	{
	}
}

namespace flash.net
{
	public class URLLoader
	{
	}
	public class URLRequest
	{
	}
	public class FileFilter
	{
	}
	public class FileReference
	{
	}
}

namespace flash.system
{
	public class System
	{
	}
	public class LoaderContext
	{
	}
}

namespace flash.text
{
	public class TextField
	{
	}
}

namespace flash.utils
{
	public class ByteArray : List<byte>
	{
		public int length {
			get { return Count; }
		}
	}
}


// __Global__

public class Vector<T> : List<T>
{
	public Vector ()
	{
	}

	public Vector (object capacity)
		: base (Convert.ToInt32 (capacity))
	{
	}

	public Vector (object capacity, bool fixedSize)
		: base (Convert.ToInt32 (capacity))
	{
	}

	public int length {
		get { return Count; }
	}
	
	public int indexOf (T value)
	{
		return IndexOf (value);
	}
	
	public void splice (int start, int count, params T [] args)
	{
		throw new NotImplementedException ();
	}
	
	public void unshift (params T [] args)
	{
		throw new NotImplementedException ();
	}
}

public class Function
{
}

public class Error
{
}

public class GlobalContext
{
	public static int getTimer ()
	{
		throw new NotImplementedException ();
	}
}

public class Object
{
//	public virtual String toString ()
//	{
//		return obj.ToString ();
//	}
}

public interface Array : IList<object>
{
}

public class RegExp
{
}

public class EventAttribute : Attribute
{
}

public static class Extensions
{
	public static void push<T> (this IList<T> arr, T o)
	{
		arr.Add (o);
	}

	public static T pop<T> (this IList<T> arr)
	{
		T v = arr [arr.Count - 1];
		arr.RemoveAt (arr.Count - 1);
		return v;
	}
}

namespace org.si.utils
{
	public partial class timer
	{
		public static object getTimer ()
		{
			throw new NotImplementedException ();
		}
	}
}

namespace org.si.sion.sequencer._base
{
	public partial class MMLParser
	{
		public static object getTimer ()
		{
			throw new NotImplementedException ();
		}
	}
}

namespace org.si.sion
{
	public partial class SiONDriver
	{
		public static object getTimer ()
		{
			throw new NotImplementedException ();
		}
	}
}
