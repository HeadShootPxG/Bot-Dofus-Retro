﻿using Bot_Dofus_1._29._1.Otros.Scripts.Acciones;
using Bot_Dofus_1._29._1.Utilidades.Configuracion;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;

namespace Bot_Dofus_1._29._1.Otros.Grupos
{
    public class Grupo : IDisposable
    {
        private Agrupamiento agrupamiento;
        private Dictionary<Cuenta, ManualResetEvent> cuentas_acabadas;

        public Cuenta lider { get; private set; }
        public ObservableCollection<Cuenta> miembros { get; private set; }
        private bool disposed;

        public Grupo(Cuenta _lider)
        {
            agrupamiento = new Agrupamiento(this);
            cuentas_acabadas = new Dictionary<Cuenta, ManualResetEvent>();
            lider = _lider;
            miembros = new ObservableCollection<Cuenta>();

            lider.grupo = this;
        }

        public void agregar_Miembro(Cuenta miembro)
        {
            if (miembros.Count >= 7)//dofus solo se pueden 8 personaje en un grupo
                return;

            miembro.grupo = this;
            miembros.Add(miembro);
            cuentas_acabadas.Add(miembro, new ManualResetEvent(false));
        }

        public void conectar_Cuentas()
        {
            lider.conexion.conexion_Servidor(IPAddress.Parse(GlobalConf.ip_conexion), GlobalConf.puerto_conexion);

            for (int i = 0; i < miembros.Count; i++)
            {
                miembros[i].conexion.conexion_Servidor(IPAddress.Parse(GlobalConf.ip_conexion), GlobalConf.puerto_conexion);
            }
        }

        #region Acciones
        public void enqueue_Acciones_Miembros(AccionesScript accion, bool iniciar_dequeue = false)
        {
            if (accion is PeleasAccion)
            {
                for (int i = 0; i < miembros.Count; i++)
                    cuentas_acabadas[miembros[i]].Set();
                return;
            }

            for (int i = 0; i < miembros.Count; i++)
                miembros[i].script.manejar_acciones.enqueue_Accion(accion, iniciar_dequeue);

            if (iniciar_dequeue)
            {
                for (int i = 0; i < miembros.Count; i++)
                    cuentas_acabadas[miembros[i]].Reset();
            }
        }

        public void esperar_Acciones_Terminadas() => WaitHandle.WaitAll(cuentas_acabadas.Values.ToArray());

        private void miembro_Acciones_Acabadas(Cuenta cuenta)
        {
            cuenta.logger.log_informacion("GRUPO", "Acciones acabadas");
            cuentas_acabadas[cuenta].Set();
        }
        #endregion

        #region Zona Dispose
        public void Dispose() => Dispose(true);
        ~Grupo() => Dispose(false);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    agrupamiento.Dispose();
                    lider.Dispose();

                    for (int i = 0; i < miembros.Count; i++)
                    {
                        miembros[i].Dispose();
                    }
                }

                agrupamiento = null;
                cuentas_acabadas.Clear();
                cuentas_acabadas = null;
                miembros.Clear();
                miembros = null;
                lider = null;

                disposed = true;
            }
        }
        #endregion
    }
}