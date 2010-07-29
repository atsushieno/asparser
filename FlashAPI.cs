using System;

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
}

namespace flash.errors
{
}

namespace flash.events
{
	public class Event : global::Object
	{
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
}

namespace flash.system
{
	public class System
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
	public class ByteArray
	{
	}
}


// __Global__

public class Vector<T>
{
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
	public virtual String toString ()
	{
		return obj.ToString ();
	}
}

public class Array
{
}

public class RegExp
{
}

public class EventAttribute : Attribute
{
}
