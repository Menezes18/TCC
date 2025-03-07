using System;
using System.Collections.Generic;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;

namespace Utp
{
    public class RelayUtils
    {
        /// <summary>
        /// Constrói o RelayServerData necessário para criar um RelayNetworkParameter para um host.
        /// </summary>
        /// <param name="allocation">A alocação para o servidor Relay.</param>
        /// <param name="connectionType">O tipo de conexão para o servidor Relay.</param>
        /// <returns>O RelayServerData.</returns>
        public static RelayServerData HostRelayData(Allocation allocation, RelayServerEndpoint.NetworkOptions connectionType)
        {
            // Obter a string a partir do tipo de conexão
            string connectionTypeString = GetStringFromConnectionType(connectionType);

            if (String.IsNullOrEmpty(connectionTypeString))
            {
                throw new ArgumentException($"ConnectionType {connectionType} é inválido");
            }

            // Seleciona o endpoint baseado no tipo de conexão desejado
            var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionTypeString);

            if (endpoint == null)
            {
                throw new ArgumentException($"Endpoint para connectionType {connectionType} não encontrado");
            }

            // Prepara o endpoint do servidor usando o IP e a porta do servidor Relay
            var serverEndpoint = NetworkEndpoint.Parse(endpoint.Host, (ushort)endpoint.Port);

            // UTP usa ponteiros em vez de arrays gerenciados para desempenho, então usamos funções auxiliares para conversão
            var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
            var connectionData = ConvertConnectionData(allocation.ConnectionData);
            var key = ConvertFromHMAC(allocation.Key);

            // Prepara os dados do servidor Relay.
            // Nas versões atuais da API, o nonce é calculado automaticamente, portanto não é necessário chamar ComputeNewNonce().
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
                ref connectionData, ref key, connectionTypeString == "dtls");
            // relayServerData.ComputeNewNonce(); // Não necessário na versão atual

            return relayServerData;
        }

        /// <summary>
        /// Constrói o RelayServerData necessário para criar um RelayNetworkParameter para um jogador.
        /// </summary>
        /// <param name="allocation">A JoinAllocation para o servidor Relay.</param>
        /// <param name="connectionType">O tipo de conexão para o servidor Relay.</param>
        /// <returns>O RelayServerData.</returns>
        public static RelayServerData PlayerRelayData(JoinAllocation allocation, RelayServerEndpoint.NetworkOptions connectionType)
        {
            // Obter a string a partir do tipo de conexão
            string connectionTypeString = GetStringFromConnectionType(connectionType);

            if (String.IsNullOrEmpty(connectionTypeString))
            {
                throw new ArgumentException($"ConnectionType {connectionType} é inválido");
            }

            // Seleciona o endpoint baseado no tipo de conexão desejado
            var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionTypeString);

            if (endpoint == null)
            {
                throw new ArgumentException($"Endpoint para connectionType {connectionType} não encontrado");
            }

            // Prepara o endpoint do servidor usando o IP e a porta do servidor Relay
            var serverEndpoint = NetworkEndpoint.Parse(endpoint.Host, (ushort)endpoint.Port);

            // UTP usa ponteiros em vez de arrays gerenciados para desempenho, então usamos funções auxiliares para conversão
            var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
            var connectionData = ConvertConnectionData(allocation.ConnectionData);
            var hostConnectionData = ConvertConnectionData(allocation.HostConnectionData);
            var key = ConvertFromHMAC(allocation.Key);

            // Prepara os dados do servidor Relay.
            // Nas versões atuais da API, o nonce é calculado automaticamente, portanto não é necessário chamar ComputeNewNonce().
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
                ref hostConnectionData, ref key, connectionTypeString == "dtls");
            // relayServerData.ComputeNewNonce(); // Não necessário na versão atual

            return relayServerData;
        }

        /// <summary>
        /// Converte o tipo de conexão para sua representação em string.
        /// </summary>
        /// <param name="connectionType">O tipo de conexão.</param>
        /// <returns>A string correspondente ao tipo de conexão.</returns>
        private static string GetStringFromConnectionType(RelayServerEndpoint.NetworkOptions connectionType)
        {
            switch (connectionType)
            {
                case RelayServerEndpoint.NetworkOptions.Tcp: return "tcp";
                case RelayServerEndpoint.NetworkOptions.Udp: return "udp";
                default: return String.Empty;
            }
        }

        #region Métodos Auxiliares

        private static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
        {
            unsafe
            {
                fixed (byte* ptr = allocationIdBytes)
                {
                    return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
                }
            }
        }

        private static RelayConnectionData ConvertConnectionData(byte[] connectionData)
        {
            unsafe
            {
                fixed (byte* ptr = connectionData)
                {
                    return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
                }
            }
        }

        private static RelayHMACKey ConvertFromHMAC(byte[] hmac)
        {
            unsafe
            {
                fixed (byte* ptr = hmac)
                {
                    return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
                }
            }
        }

        private static RelayServerEndpoint GetEndpointForConnectionType(List<RelayServerEndpoint> endpoints, string connectionType)
        {
            foreach (var endpoint in endpoints)
            {
                if (endpoint.ConnectionType == connectionType)
                {
                    return endpoint;
                }
            }
            return null;
        }

        #endregion
    }
}
