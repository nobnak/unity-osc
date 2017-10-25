using System;

public static class NtpUtil {
	public static double Delay(DateTime t0, DateTime t1, DateTime t2, DateTime t3) {
		return ((t1 - t0) - (t3 - t2)).TotalSeconds * 0.5;
	}
	public static double Roundtrip(DateTime t0, DateTime t1, DateTime t2, DateTime t3) {
		return ((t3 - t0) - (t2 - t1)).TotalSeconds;
	}
}
