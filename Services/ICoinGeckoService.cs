using System.Collections.Generic;
using System.Threading.Tasks;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para obtener precios de criptomonedas usando CoinGecko API.
/// </summary>
public interface ICoinGeckoService
{
    Task<decimal?> GetCryptoPriceAsync(string coinId);
    Task<CryptoFullInfo?> GetCryptoFullInfoAsync(string coinId);
    Task<List<CryptoFullInfo>> GetTopCryptosAsync(int limit = 10);
    Task<List<CoinInfo>> SearchCoinsAsync(string query);
}
