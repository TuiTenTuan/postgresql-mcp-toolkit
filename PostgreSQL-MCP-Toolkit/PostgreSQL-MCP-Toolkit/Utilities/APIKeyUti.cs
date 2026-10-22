using System.Security.Cryptography;
using System.Text;

public class APIKeyUti
{
	private static APIKeyUti? instance;

	public static APIKeyUti Instance
	{
        get { if (instance == null) instance = new APIKeyUti(); return instance; }
        private set { instance = value; }
    }

	private APIKeyUti() { }

    public bool Equals(string provided, string expected)
    {
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
    }
}